using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.Delegations;

public sealed record ApproverDelegationDto(
    Guid Id,
    Guid DelegatorUserId,
    string DelegatorName,
    Guid DelegateUserId,
    string DelegateName,
    Guid? ProjectOid,
    Guid? SubcontractorId,
    string? ApproverRole,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidTo,
    bool IsActive);

public sealed record CreateApproverDelegationCommand(
    Guid DelegateUserId,
    Guid? ProjectOid,
    Guid? SubcontractorId,
    string? ApproverRole,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidTo) : IRequest<ApproverDelegationDto>;

public sealed record RevokeApproverDelegationCommand(Guid Id) : IRequest<Unit>;

public sealed record ListApproverDelegationsQuery(bool ActiveOnly = true) : IRequest<IReadOnlyList<ApproverDelegationDto>>;

public sealed class CreateApproverDelegationHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IAuditRecorder audit) : IRequestHandler<CreateApproverDelegationCommand, ApproverDelegationDto>
{
    public async Task<ApproverDelegationDto> Handle(CreateApproverDelegationCommand req, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        if (req.ValidTo <= req.ValidFrom)
            throw new ValidationFailedException("Дата окончания должна быть позже даты начала.");

        var entity = new ApproverDelegation
        {
            DelegatorUserId = userId,
            DelegateUserId = req.DelegateUserId,
            ProjectOid = req.ProjectOid,
            SubcontractorId = req.SubcontractorId,
            ApproverRole = req.ApproverRole,
            ValidFrom = req.ValidFrom,
            ValidTo = req.ValidTo,
            CreatedByUserId = userId
        };
        db.ApproverDelegations.Add(entity);
        audit.Record(new AuditEntry(
            AuditActions.DelegationCreated, "approver_delegation", entity.Id,
            $"Замещение: {req.DelegateUserId} до {req.ValidTo:dd.MM.yyyy}"));
        await db.SaveChangesAsync(ct);
        return await MapAsync(db, entity.Id, ct);
    }

    internal static async Task<ApproverDelegationDto> MapAsync(ITansuDbContext db, Guid id, CancellationToken ct)
    {
        var d = await db.ApproverDelegations.AsNoTracking()
            .Include(x => x.Delegator)
            .Include(x => x.Delegate)
            .FirstAsync(x => x.Id == id, ct);
        return new ApproverDelegationDto(
            d.Id, d.DelegatorUserId, d.Delegator!.FullName, d.DelegateUserId, d.Delegate!.FullName,
            d.ProjectOid, d.SubcontractorId, d.ApproverRole, d.ValidFrom, d.ValidTo, d.IsActive);
    }
}

public sealed class RevokeApproverDelegationHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IAuditRecorder audit) : IRequestHandler<RevokeApproverDelegationCommand, Unit>
{
    public async Task<Unit> Handle(RevokeApproverDelegationCommand req, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        var d = await db.ApproverDelegations.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new NotFoundException("ApproverDelegation", req.Id);
        if (d.DelegatorUserId != userId && !currentUser.IsSuperUser)
            throw new ForbiddenException("Нельзя отозвать чужое замещение.");
        d.IsActive = false;
        audit.Record(new AuditEntry(AuditActions.DelegationRevoked, "approver_delegation", d.Id, "Замещение отозвано"));
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public sealed class ListApproverDelegationsHandler(
    ITansuDbContext db,
    ICurrentUser currentUser) : IRequestHandler<ListApproverDelegationsQuery, IReadOnlyList<ApproverDelegationDto>>
{
    public async Task<IReadOnlyList<ApproverDelegationDto>> Handle(ListApproverDelegationsQuery req, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        var q = db.ApproverDelegations.AsNoTracking()
            .Include(x => x.Delegator)
            .Include(x => x.Delegate)
            .Where(x => x.DelegatorUserId == userId || x.DelegateUserId == userId || currentUser.IsSuperUser);
        if (req.ActiveOnly)
            q = q.Where(x => x.IsActive && x.ValidTo >= DateTimeOffset.UtcNow);
        var list = await q.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return list.Select(d => new ApproverDelegationDto(
            d.Id, d.DelegatorUserId, d.Delegator!.FullName, d.DelegateUserId, d.Delegate!.FullName,
            d.ProjectOid, d.SubcontractorId, d.ApproverRole, d.ValidFrom, d.ValidTo, d.IsActive)).ToList();
    }
}
