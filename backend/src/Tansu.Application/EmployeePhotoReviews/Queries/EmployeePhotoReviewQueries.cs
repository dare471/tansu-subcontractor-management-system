using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.EmployeePhotoReviews.Queries;

public sealed record GetEmployeePhotoReviewStatusQuery(Guid EmployeeId) : IRequest<EmployeePhotoReviewStatusDto>;

public sealed class GetEmployeePhotoReviewStatusHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetEmployeePhotoReviewStatusQuery, EmployeePhotoReviewStatusDto>
{
    public async Task<EmployeePhotoReviewStatusDto> Handle(GetEmployeePhotoReviewStatusQuery req, CancellationToken ct)
    {
        await EnsureReadAccessAsync(req.EmployeeId, currentUser, db, ct);

        var employee = await db.Employees.AsNoTracking()
            .FirstAsync(e => e.Id == req.EmployeeId, ct);

        var history = await LoadHistoryAsync(db, req.EmployeeId, ct);
        return EmployeePhotoReviewMapper.ToStatusDto(employee, history);
    }

    internal static async Task EnsureReadAccessAsync(
        Guid employeeId,
        ICurrentUser currentUser,
        ITansuDbContext db,
        CancellationToken ct)
    {
        var employee = await db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct)
            ?? throw new Common.Exceptions.NotFoundException("Employee", employeeId);

        if (currentUser.UserType == UserType.Employee && currentUser.EmployeeId != employeeId)
            throw new Common.Exceptions.ForbiddenException();

        if (currentUser.UserType == UserType.Subcontractor &&
            currentUser.SubcontractorId != employee.SubcontractorId)
            throw new Common.Exceptions.ForbiddenException("Сотрудник принадлежит другому субподрядчику.");
    }

    internal static async Task<IReadOnlyList<EmployeePhotoReviewDto>> LoadHistoryAsync(
        ITansuDbContext db,
        Guid employeeId,
        CancellationToken ct) =>
        await db.EmployeePhotoReviews.AsNoTracking()
            .Where(r => r.EmployeeId == employeeId)
            .Include(r => r.ReviewedBy)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new EmployeePhotoReviewDto(
                r.Id,
                r.EmployeeId,
                r.PhotoPath,
                r.ReviewType,
                r.Result,
                r.Reason,
                r.ReviewedBy != null ? r.ReviewedBy.FullName : null,
                r.CreatedAt))
            .ToListAsync(ct);
}

public sealed record ListPendingPhotoReviewsQuery : IRequest<IReadOnlyList<PendingPhotoReviewItemDto>>;

public sealed class ListPendingPhotoReviewsHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService) : IRequestHandler<ListPendingPhotoReviewsQuery, IReadOnlyList<PendingPhotoReviewItemDto>>
{
    public async Task<IReadOnlyList<PendingPhotoReviewItemDto>> Handle(ListPendingPhotoReviewsQuery req, CancellationToken ct)
    {
        await EmployeePhotoReviewAuthorization.EnsureCanReviewPhotosAsync(currentUser, accessService, ct);

        var access = await accessService.GetAccessAsync(ct);

        var q = db.Employees.AsNoTracking()
            .Where(e => e.PhotoReviewStatus == EmployeePhotoReviewStatus.Pending && e.PhotoPath != null);

        if (access.VisibleSubcontractorIds is { } subs)
            q = q.Where(e => subs.Contains(e.SubcontractorId));
        if (access.VisibleProjectOids is { } projects)
            q = q.Where(e => projects.Contains(e.ProjectOid));

        return await q
            .Include(e => e.Subcontractor)
            .Include(e => e.Project)
            .Include(e => e.PhotoUploadedBy)
            .OrderBy(e => e.UpdatedAt)
            .Select(e => new PendingPhotoReviewItemDto(
                e.Id,
                e.FullName,
                e.Position,
                e.Subcontractor!.Name,
                e.Project!.Name,
                e.PhotoPath!,
                e.UpdatedAt,
                e.PhotoUploadedByUserId,
                e.PhotoUploadedBy != null ? e.PhotoUploadedBy.FullName : null,
                e.PhotoUploadedBy != null ? e.PhotoUploadedBy.Email : null,
                e.PhotoUploadedBy != null ? e.PhotoUploadedBy.UserType : null))
            .ToListAsync(ct);
    }
}
