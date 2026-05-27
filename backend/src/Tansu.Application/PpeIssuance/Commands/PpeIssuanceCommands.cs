using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.PpeIssuance.Commands;

public sealed record IssueEmployeePpeCommand(
    Guid EmployeeId,
    string ItemType,
    string? Size,
    string? InventoryNumber,
    string? Notes) : IRequest<EmployeePpeIssuanceDto>;

public sealed class IssueEmployeePpeHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<IssueEmployeePpeCommand, EmployeePpeIssuanceDto>
{
    public async Task<EmployeePpeIssuanceDto> Handle(IssueEmployeePpeCommand req, CancellationToken ct)
    {
        await PpeIssuanceAuthorization.EnsureEmployeeAccessAsync(req.EmployeeId, currentUser, db, ct);

        var itemType = req.ItemType.Trim().ToLowerInvariant();
        if (!PpeItemType.All.Contains(itemType))
            throw new ValidationFailedException("Недопустимый тип СИЗ. Доступны: helmet, uniform.");

        var issuerId = currentUser.UserId ?? throw new UnauthorizedException();
        if (currentUser.UserType == UserType.Employee)
            throw new ForbiddenException("Сотрудник не может выдавать СИЗ себе.");

        var employee = await db.Employees.AsNoTracking()
            .FirstAsync(e => e.Id == req.EmployeeId, ct);

        var now = DateTimeOffset.UtcNow;
        var activeSameType = await db.EmployeePpeIssuances
            .Where(p => p.EmployeeId == req.EmployeeId && p.ItemType == itemType && p.ReturnedAt == null)
            .ToListAsync(ct);
        foreach (var prev in activeSameType)
            prev.ReturnedAt = now;

        var issuance = new EmployeePpeIssuance
        {
            EmployeeId = req.EmployeeId,
            ItemType = itemType,
            Size = NormalizeOptional(req.Size),
            InventoryNumber = NormalizeOptional(req.InventoryNumber),
            Notes = NormalizeOptional(req.Notes),
            IssuedAt = now,
            IssuedByUserId = issuerId
        };
        db.EmployeePpeIssuances.Add(issuance);
        await db.SaveChangesAsync(ct);

        var issuer = await db.Users.AsNoTracking().FirstAsync(u => u.Id == issuerId, ct);
        issuance.IssuedBy = issuer;
        issuance.Employee = employee;

        return PpeIssuanceMapper.ToDto(issuance);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record ReturnEmployeePpeCommand(Guid EmployeeId, Guid IssuanceId, string? Notes)
    : IRequest<EmployeePpeIssuanceDto>;

public sealed class ReturnEmployeePpeHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<ReturnEmployeePpeCommand, EmployeePpeIssuanceDto>
{
    public async Task<EmployeePpeIssuanceDto> Handle(ReturnEmployeePpeCommand req, CancellationToken ct)
    {
        await PpeIssuanceAuthorization.EnsureEmployeeAccessAsync(req.EmployeeId, currentUser, db, ct);
        if (currentUser.UserType == UserType.Employee)
            throw new ForbiddenException("Сотрудник не может оформить возврат СИЗ.");

        var row = await db.EmployeePpeIssuances
            .Include(p => p.IssuedBy)
            .FirstOrDefaultAsync(p => p.Id == req.IssuanceId && p.EmployeeId == req.EmployeeId, ct)
            ?? throw new NotFoundException("EmployeePpeIssuance", req.IssuanceId);

        if (row.ReturnedAt is not null)
            throw new ConflictException("already_returned", "СИЗ уже возвращено.");

        row.ReturnedAt = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(req.Notes))
            row.Notes = string.IsNullOrWhiteSpace(row.Notes)
                ? req.Notes.Trim()
                : $"{row.Notes}; возврат: {req.Notes.Trim()}";

        await db.SaveChangesAsync(ct);
        return PpeIssuanceMapper.ToDto(row);
    }
}
