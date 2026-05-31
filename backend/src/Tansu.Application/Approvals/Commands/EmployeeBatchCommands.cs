using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Approvals;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.EmployeePortal.Commands;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.Approvals.Commands;

public sealed record CreateEmployeeBatchCommand(Guid ProjectOid, string Title) : IRequest<ApprovalBatchDto>;

public sealed class CreateEmployeeBatchValidator : AbstractValidator<CreateEmployeeBatchCommand>
{
    public CreateEmployeeBatchValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
    }
}

public sealed class CreateEmployeeBatchHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<CreateEmployeeBatchCommand, ApprovalBatchDto>
{
    public async Task<ApprovalBatchDto> Handle(CreateEmployeeBatchCommand req, CancellationToken ct)
    {
        var sid = currentUser.SubcontractorId
            ?? throw new ForbiddenException("Контекст субподрядчика отсутствует.");
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var hasBinding = await db.ProjectSubcontractors.AnyAsync(
            ps => ps.ProjectOid == req.ProjectOid && ps.SubcontractorId == sid, ct);
        if (!hasBinding)
            throw new ValidationFailedException("Субподрядчик не привязан к этому проекту.");

        var batch = new EmployeeApprovalBatch
        {
            SubcontractorId = sid,
            ProjectOid = req.ProjectOid,
            CreatedByUserId = userId,
            Title = req.Title.Trim(),
            Status = BatchStatus.Draft,
            EmployeeCount = 0
        };
        db.EmployeeApprovalBatches.Add(batch);
        await db.SaveChangesAsync(ct);

        batch = await db.EmployeeApprovalBatches
            .Include(b => b.Project)
            .FirstAsync(b => b.Id == batch.Id, ct);

        return EmployeeBatchCore.ToDto(batch);
    }
}

public sealed record ListEmployeeBatchesQuery : IRequest<IReadOnlyList<ApprovalBatchDto>>;

public sealed class ListEmployeeBatchesHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<ListEmployeeBatchesQuery, IReadOnlyList<ApprovalBatchDto>>
{
    public async Task<IReadOnlyList<ApprovalBatchDto>> Handle(ListEmployeeBatchesQuery req, CancellationToken ct)
    {
        var sid = currentUser.SubcontractorId
            ?? throw new ForbiddenException("Контекст субподрядчика отсутствует.");

        var batches = await db.EmployeeApprovalBatches.AsNoTracking()
            .Include(b => b.Project)
            .Include(b => b.Members)
            .ThenInclude(m => m.Employee)
            .Where(b => b.SubcontractorId == sid)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

        if (batches.Count == 0)
            return Array.Empty<ApprovalBatchDto>();

        var employeeIds = batches.SelectMany(b => b.Members.Select(m => m.EmployeeId)).Distinct().ToList();
        var statuses = await EmployeeBatchCore.LoadMemberStatusesAsync(db, employeeIds, ct);

        return batches.Select(b => EmployeeBatchCore.ToDto(b, statuses)).ToList();
    }
}

public sealed record GetEmployeeBatchQuery(Guid BatchId) : IRequest<ApprovalBatchDto>;

public sealed class GetEmployeeBatchHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetEmployeeBatchQuery, ApprovalBatchDto>
{
    public async Task<ApprovalBatchDto> Handle(GetEmployeeBatchQuery req, CancellationToken ct)
    {
        var batch = await EmployeeBatchCore.LoadOwnedBatchAsync(db, req.BatchId, currentUser, ct);
        var statuses = await EmployeeBatchCore.LoadMemberStatusesAsync(
            db, batch.Members.Select(m => m.EmployeeId), ct);
        return EmployeeBatchCore.ToDto(batch, statuses);
    }
}

public sealed record AddEmployeesToBatchCommand(Guid BatchId, IReadOnlyList<Guid> EmployeeIds)
    : IRequest<ApprovalBatchDto>;

public sealed class AddEmployeesToBatchHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<AddEmployeesToBatchCommand, ApprovalBatchDto>
{
    public async Task<ApprovalBatchDto> Handle(AddEmployeesToBatchCommand req, CancellationToken ct)
    {
        var batch = await EmployeeBatchCore.LoadOwnedBatchAsync(db, req.BatchId, currentUser, ct);
        await EmployeeBatchCore.EnsureDraftAsync(batch);

        if (req.EmployeeIds.Count == 0)
            throw new ValidationFailedException("Выберите хотя бы одного сотрудника.");

        var sid = currentUser.SubcontractorId!.Value;
        var employees = await db.Employees
            .Where(e => req.EmployeeIds.Contains(e.Id) && e.SubcontractorId == sid)
            .ToListAsync(ct);

        if (employees.Count != req.EmployeeIds.Count)
            throw new NotFoundException("Employee", "one or more ids");

        foreach (var employee in employees)
        {
            if (employee.ProjectOid != batch.ProjectOid)
            {
                throw new ValidationFailedException(
                    $"Сотрудник «{employee.FullName}» относится к другому проекту.");
            }

            await EmployeeSubmitCore.EnsureSubmittableAsync(db, employee, batch.Id, ct);

            if (batch.Members.Any(m => m.EmployeeId == employee.Id))
                continue;

            db.EmployeeApprovalBatchMembers.Add(new EmployeeApprovalBatchMember
            {
                BatchId = batch.Id,
                EmployeeId = employee.Id
            });
        }

        batch.EmployeeCount = await db.EmployeeApprovalBatchMembers
            .CountAsync(m => m.BatchId == batch.Id, ct);
        await db.SaveChangesAsync(ct);

        batch = await EmployeeBatchCore.LoadOwnedBatchAsync(db, batch.Id, currentUser, ct);
        var statuses = await EmployeeBatchCore.LoadMemberStatusesAsync(
            db, batch.Members.Select(m => m.EmployeeId), ct);
        return EmployeeBatchCore.ToDto(batch, statuses);
    }
}

public sealed record RemoveEmployeeFromBatchCommand(Guid BatchId, Guid EmployeeId)
    : IRequest<ApprovalBatchDto>;

public sealed class RemoveEmployeeFromBatchHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<RemoveEmployeeFromBatchCommand, ApprovalBatchDto>
{
    public async Task<ApprovalBatchDto> Handle(RemoveEmployeeFromBatchCommand req, CancellationToken ct)
    {
        var batch = await EmployeeBatchCore.LoadOwnedBatchAsync(db, req.BatchId, currentUser, ct);
        await EmployeeBatchCore.EnsureDraftAsync(batch);

        var member = batch.Members.FirstOrDefault(m => m.EmployeeId == req.EmployeeId)
            ?? throw new NotFoundException("EmployeeApprovalBatchMember", req.EmployeeId);

        db.EmployeeApprovalBatchMembers.Remove(member);
        batch.EmployeeCount = Math.Max(0, batch.EmployeeCount - 1);
        await db.SaveChangesAsync(ct);

        batch = await EmployeeBatchCore.LoadOwnedBatchAsync(db, batch.Id, currentUser, ct);
        var statuses = await EmployeeBatchCore.LoadMemberStatusesAsync(
            db, batch.Members.Select(m => m.EmployeeId), ct);
        return EmployeeBatchCore.ToDto(batch, statuses);
    }
}

public sealed record DeleteEmployeeBatchCommand(Guid BatchId) : IRequest;

public sealed class DeleteEmployeeBatchHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<DeleteEmployeeBatchCommand>
{
    public async Task Handle(DeleteEmployeeBatchCommand req, CancellationToken ct)
    {
        var batch = await EmployeeBatchCore.LoadOwnedBatchAsync(db, req.BatchId, currentUser, ct);
        await EmployeeBatchCore.EnsureDraftAsync(batch);

        db.EmployeeApprovalBatches.Remove(batch);
        await db.SaveChangesAsync(ct);
    }
}

public sealed record SubmitEmployeeBatchCommand(Guid BatchId) : IRequest<BatchSubmitResultDto>;

public sealed class SubmitEmployeeBatchHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IPublishEndpoint publisher,
    IMediator mediator)
    : IRequestHandler<SubmitEmployeeBatchCommand, BatchSubmitResultDto>
{
    public async Task<BatchSubmitResultDto> Handle(SubmitEmployeeBatchCommand req, CancellationToken ct)
    {
        var batch = await EmployeeBatchCore.LoadOwnedBatchAsync(db, req.BatchId, currentUser, ct);
        await EmployeeBatchCore.EnsureDraftAsync(batch);

        if (batch.Members.Count == 0)
            throw new ValidationFailedException("Добавьте сотрудников в пакет перед отправкой.");

        var initiatorId = currentUser.UserId ?? throw new UnauthorizedException();
        var initiator = await db.Users.FirstAsync(u => u.Id == initiatorId, ct);

        var employees = batch.Members
            .Select(m => m.Employee!)
            .OrderBy(e => e.FullName)
            .ToList();

        EmployeeSubmitCore.PreparedSubmission? firstPrepared = null;
        var items = new List<BatchSubmitItemDto>();

        foreach (var employee in employees)
        {
            await EmployeeSubmitCore.EnsurePhotoApprovedAsync(db, employee, mediator, ct);
            await EmployeeSubmitCore.EnsureSubmittableAsync(db, employee, batch.Id, ct);
            var prepared = await EmployeeSubmitCore.PrepareSubmissionAsync(
                db, employee, initiatorId, batch.Id, ct);

            db.ApprovalSheet.AddRange(prepared.Sheets);
            firstPrepared ??= prepared;

            items.Add(new BatchSubmitItemDto(employee.Id, prepared.RoundId));
        }

        batch.Status = BatchStatus.Submitted;
        batch.SubmittedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        if (firstPrepared is not null)
        {
            await EmployeeBatchCore.PublishBatchNotificationsAsync(
                publisher, batch, initiator, firstPrepared.FirstApprover, employees, ct);
        }

        foreach (var item in items)
            await mediator.Send(new ProvisionEmployeePortalCommand(item.EmployeeId), ct);

        return new BatchSubmitResultDto(batch.Id, batch.Title, items.Count, items);
    }
}
