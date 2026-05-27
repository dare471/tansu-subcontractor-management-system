using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tansu.Application.AccessPasses.Commands;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.AccessPasses.Queries;

public sealed record GetEmployeeAccessPassQuery(Guid EmployeeId) : IRequest<EmployeeAccessPassDto?>;

public sealed class GetEmployeeAccessPassHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IOptions<AccessPassOptions> options) : IRequestHandler<GetEmployeeAccessPassQuery, EmployeeAccessPassDto?>
{
    public async Task<EmployeeAccessPassDto?> Handle(GetEmployeeAccessPassQuery req, CancellationToken ct)
    {
        await AccessPassAuthorization.EnsureEmployeeAccessAsync(req.EmployeeId, currentUser, db, ct);

        var pass = await db.EmployeeAccessPasses.AsNoTracking()
            .Where(p => p.EmployeeId == req.EmployeeId && p.RevokedAt == null)
            .OrderByDescending(p => p.IssuedAt)
            .FirstOrDefaultAsync(ct);
        if (pass is null)
            return null;

        var employee = await db.Employees.AsNoTracking()
            .FirstAsync(e => e.Id == req.EmployeeId, ct);

        return AccessPassMapper.ToDto(pass, employee.PhotoPath, options.Value.VerifyWebBaseUrl);
    }
}

public sealed record GetAccessPassByTokenQuery(string Token) : IRequest<AccessPassLookupDto?>;

public sealed class GetAccessPassByTokenHandler(ITansuDbContext db)
    : IRequestHandler<GetAccessPassByTokenQuery, AccessPassLookupDto?>
{
    public async Task<AccessPassLookupDto?> Handle(GetAccessPassByTokenQuery req, CancellationToken ct)
    {
        var token = req.Token.Trim();
        if (string.IsNullOrEmpty(token))
            return null;

        var pass = await db.EmployeeAccessPasses.AsNoTracking()
            .Include(p => p.Employee!)
            .ThenInclude(e => e!.Subcontractor)
            .Include(p => p.Employee!)
            .ThenInclude(e => e!.Project)
            .FirstOrDefaultAsync(p => p.Token == token, ct);

        if (pass?.Employee is not { } employee)
            return null;

        return new AccessPassLookupDto(
            employee.Id,
            employee.FullName,
            employee.Position,
            employee.Subcontractor?.Name ?? "—",
            employee.Project?.Name,
            !string.IsNullOrEmpty(employee.PhotoPath),
            pass.IssuedAt,
            pass.RevokedAt is null);
    }
}

public sealed record GetAccessPassQrQuery(Guid EmployeeId) : IRequest<byte[]?>;

public sealed class GetAccessPassQrHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IAccessPassQrEncoder qrEncoder,
    IOptions<AccessPassOptions> options) : IRequestHandler<GetAccessPassQrQuery, byte[]?>
{
    public async Task<byte[]?> Handle(GetAccessPassQrQuery req, CancellationToken ct)
    {
        await AccessPassAuthorization.EnsureEmployeeAccessAsync(req.EmployeeId, currentUser, db, ct);

        var pass = await db.EmployeeAccessPasses.AsNoTracking()
            .Where(p => p.EmployeeId == req.EmployeeId && p.RevokedAt == null)
            .OrderByDescending(p => p.IssuedAt)
            .FirstOrDefaultAsync(ct);
        if (pass is null)
            return null;

        var payload = IssueEmployeeAccessPassHandler.BuildVerifyUrl(options.Value.VerifyWebBaseUrl, pass.Token);
        return qrEncoder.EncodePng(payload);
    }
}

internal static class AccessPassAuthorization
{
    public static async Task EnsureEmployeeAccessAsync(
        Guid employeeId,
        ICurrentUser currentUser,
        ITansuDbContext db,
        CancellationToken ct)
    {
        var employee = await db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct)
            ?? throw new NotFoundException("Employee", employeeId);

        if (currentUser.UserType == UserType.Employee &&
            currentUser.EmployeeId != employeeId)
        {
            throw new ForbiddenException();
        }

        if (currentUser.UserType == UserType.Subcontractor &&
            currentUser.SubcontractorId != employee.SubcontractorId)
        {
            throw new ForbiddenException("Сотрудник принадлежит другому субподрядчику.");
        }
    }
}

internal static class AccessPassMapper
{
    public static EmployeeAccessPassDto ToDto(
        Domain.Entities.EmployeeAccessPass pass,
        string? photoPath,
        string verifyWebBaseUrl) =>
        new(
            pass.Id,
            pass.EmployeeId,
            pass.Token,
            IssueEmployeeAccessPassHandler.BuildVerifyUrl(verifyWebBaseUrl, pass.Token),
            pass.IssuedAt,
            !string.IsNullOrEmpty(photoPath));
}
