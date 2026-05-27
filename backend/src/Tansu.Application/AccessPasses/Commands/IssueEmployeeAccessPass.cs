using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tansu.Application.AccessPasses;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;

namespace Tansu.Application.AccessPasses.Commands;

public sealed record IssueEmployeeAccessPassCommand(Guid EmployeeId) : IRequest<EmployeeAccessPassDto?>;

public sealed class IssueEmployeeAccessPassHandler(
    ITansuDbContext db,
    IAccessPassTokenGenerator tokenGenerator,
    IOptions<AccessPassOptions> options) : IRequestHandler<IssueEmployeeAccessPassCommand, EmployeeAccessPassDto?>
{
    public async Task<EmployeeAccessPassDto?> Handle(IssueEmployeeAccessPassCommand req, CancellationToken ct)
    {
        var employee = await db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == req.EmployeeId, ct);
        if (employee is null)
            return null;

        var activePasses = await db.EmployeeAccessPasses
            .Where(p => p.EmployeeId == req.EmployeeId && p.RevokedAt == null)
            .ToListAsync(ct);
        var now = DateTimeOffset.UtcNow;
        foreach (var pass in activePasses)
            pass.RevokedAt = now;

        var token = tokenGenerator.GenerateToken();
        var passEntity = new EmployeeAccessPass
        {
            EmployeeId = req.EmployeeId,
            Token = token,
            IssuedAt = now
        };
        db.EmployeeAccessPasses.Add(passEntity);
        await db.SaveChangesAsync(ct);

        var verifyUrl = BuildVerifyUrl(options.Value.VerifyWebBaseUrl, token);
        return new EmployeeAccessPassDto(
            passEntity.Id,
            passEntity.EmployeeId,
            passEntity.Token,
            verifyUrl,
            passEntity.IssuedAt,
            !string.IsNullOrEmpty(employee.PhotoPath));
    }

    internal static string BuildVerifyUrl(string baseUrl, string token)
    {
        var trimmed = baseUrl.TrimEnd('/');
        return $"{trimmed}/?token={Uri.EscapeDataString(token)}";
    }
}

public sealed record RevokeEmployeeAccessPassesCommand(Guid EmployeeId) : IRequest<Unit>;

public sealed class RevokeEmployeeAccessPassesHandler(ITansuDbContext db)
    : IRequestHandler<RevokeEmployeeAccessPassesCommand, Unit>
{
    public async Task<Unit> Handle(RevokeEmployeeAccessPassesCommand req, CancellationToken ct)
    {
        var passes = await db.EmployeeAccessPasses
            .Where(p => p.EmployeeId == req.EmployeeId && p.RevokedAt == null)
            .ToListAsync(ct);
        if (passes.Count == 0)
            return Unit.Value;

        var now = DateTimeOffset.UtcNow;
        foreach (var pass in passes)
            pass.RevokedAt = now;

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
