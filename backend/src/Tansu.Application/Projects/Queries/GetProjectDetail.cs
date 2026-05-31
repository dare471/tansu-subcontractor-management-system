using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Employees;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.Projects.Queries;

public sealed record GetProjectDetailQuery(Guid ProjectOid) : IRequest<ProjectDetailDto>;

public sealed class GetProjectDetailHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService) : IRequestHandler<GetProjectDetailQuery, ProjectDetailDto>
{
    public async Task<ProjectDetailDto> Handle(GetProjectDetailQuery req, CancellationToken ct)
    {
        if (currentUser.UserType != UserType.Tansu)
            throw new ForbiddenException("Карточка проекта доступна только сотрудникам ТАНСУ.");

        var access = await accessService.GetAccessAsync(ct);
        if (access.VisibleProjectOids is { } projects && !projects.Contains(req.ProjectOid))
            throw new ForbiddenException("Проект вне вашей области видимости.");

        var project = await db.ProjectRefs.AsNoTracking()
            .Include(p => p.ResponsibleAdmin)
            .Include(p => p.ProjectManager)
            .FirstOrDefaultAsync(p => p.ProjectOid == req.ProjectOid, ct)
            ?? throw new NotFoundException("Project", req.ProjectOid);

        var subLinks = await db.ProjectSubcontractors.AsNoTracking()
            .Where(ps => ps.ProjectOid == req.ProjectOid)
            .Include(ps => ps.Subcontractor)
            .Include(ps => ps.ProgressReportedBy)
            .ToListAsync(ct);

        var subIds = subLinks.Select(x => x.SubcontractorId).ToList();
        var employees = await db.Employees.AsNoTracking()
            .Where(e => e.ProjectOid == req.ProjectOid)
            .Include(e => e.Subcontractor)
            .ToListAsync(ct);

        var employeeIds = employees.Select(e => e.Id).ToList();
        var sheets = employeeIds.Count == 0
            ? new List<ApprovalSheetEntry>()
            : await db.ApprovalSheet.AsNoTracking()
                .Where(a => employeeIds.Contains(a.EmployeeId))
                .ToListAsync(ct);

        var sheetsByEmployee = sheets
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ApprovalSheetEntry>)g.ToList());

        var approvedBySub = subIds.ToDictionary(id => id, _ => 0);
        var workforce = new List<ProjectWorkforceItemDto>();
        foreach (var employee in employees.OrderBy(e => e.Subcontractor!.Name).ThenBy(e => e.FullName))
        {
            sheetsByEmployee.TryGetValue(employee.Id, out var employeeSheets);
            employeeSheets ??= Array.Empty<ApprovalSheetEntry>();
            var status = EmployeeStatusResolver.ResolveFromSheets(employeeSheets);
            if (status == ApprovalStatus.Approved)
                approvedBySub[employee.SubcontractorId] = approvedBySub.GetValueOrDefault(employee.SubcontractorId) + 1;

            workforce.Add(new ProjectWorkforceItemDto(
                employee.Id,
                employee.FullName,
                employee.Position,
                employee.Subcontractor!.Name,
                status));
        }

        var subcontractors = subLinks
            .Where(x => x.Subcontractor is not null)
            .Select(x =>
            {
                var count = employees.Count(e => e.SubcontractorId == x.SubcontractorId);
                approvedBySub.TryGetValue(x.SubcontractorId, out var approved);
                return new ProjectSubcontractorItemDto(
                    x.SubcontractorId,
                    x.Subcontractor!.Name,
                    x.Subcontractor.Bin,
                    x.ActivityType,
                    x.CompletionPercent,
                    x.ProgressReportedAt,
                    x.ProgressReportedBy?.FullName,
                    count,
                    approved);
            })
            .OrderBy(x => x.Name)
            .ToList();

        var assignedUserIds = await db.UserProjectAssignments.AsNoTracking()
            .Where(a => a.ProjectOid == req.ProjectOid)
            .Select(a => a.UserId)
            .ToListAsync(ct);

        var teamUserIds = assignedUserIds
            .Concat(new[] { project.ResponsibleAdminUserId, project.ProjectManagerUserId }
                .Where(id => id is not null)
                .Select(id => id!.Value))
            .Distinct()
            .ToList();

        var teamUsers = teamUserIds.Count == 0
            ? new List<User>()
            : await db.Users.AsNoTracking()
                .Where(u => teamUserIds.Contains(u.Id) && u.UserType == UserType.Tansu)
                .ToListAsync(ct);

        var team = teamUsers
            .Select(u =>
            {
                var roleLabel = u.TansuRole is not null ? TansuRole.Label(u.TansuRole) : "ТАНСУ";
                if (project.ResponsibleAdminUserId == u.Id)
                    roleLabel = "Ответственный админ";
                else if (project.ProjectManagerUserId == u.Id)
                    roleLabel = "Руководитель проекта";
                return new ProjectTeamMemberDto(
                    u.Id, u.FullName, u.Email, u.Position, u.TansuRole, roleLabel);
            })
            .OrderBy(u => u.FullName)
            .ToList();

        var documents = await db.ProjectDocuments.AsNoTracking()
            .Where(d => d.ProjectOid == req.ProjectOid)
            .Include(d => d.UploadedBy)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new ProjectDocumentDto(
                d.Id,
                d.Name,
                d.DocumentType,
                ProjectDocumentType.Label(d.DocumentType),
                d.ContentType,
                d.UploadedAt,
                d.UploadedBy!.FullName))
            .ToListAsync(ct);

        return new ProjectDetailDto(
            project.ProjectOid,
            project.Name,
            subcontractors.Count,
            project.CustomerName,
            project.CustomerPhone,
            project.CustomerEmail,
            project.BudgetAmount,
            project.BudgetCurrency,
            project.ResponsibleAdminUserId,
            project.ResponsibleAdmin?.FullName,
            project.ResponsibleAdmin?.Email,
            project.ProjectManagerUserId,
            project.ProjectManager?.FullName,
            project.ProjectManager?.Email,
            subcontractors,
            workforce,
            team,
            documents);
    }
}

public sealed record ListProjectStaffOptionsQuery : IRequest<IReadOnlyList<ProjectStaffOptionDto>>;

public sealed class ListProjectStaffOptionsHandler(ITansuDbContext db, ICurrentUser currentUser)
    : IRequestHandler<ListProjectStaffOptionsQuery, IReadOnlyList<ProjectStaffOptionDto>>
{
    public async Task<IReadOnlyList<ProjectStaffOptionDto>> Handle(
        ListProjectStaffOptionsQuery req, CancellationToken ct)
    {
        if (currentUser.UserType != UserType.Tansu)
            throw new ForbiddenException();

        return await db.Users.AsNoTracking()
            .Where(u => u.UserType == UserType.Tansu && u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new ProjectStaffOptionDto(u.Id, u.FullName, u.Email, u.TansuRole))
            .ToListAsync(ct);
    }
}
