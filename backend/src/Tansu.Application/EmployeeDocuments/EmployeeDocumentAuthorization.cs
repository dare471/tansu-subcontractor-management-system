using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.EmployeeDocuments;

internal static class EmployeeDocumentAuthorization
{
    public static async Task EnsureEmployeeAccessAsync(
        Guid employeeId,
        ICurrentUser currentUser,
        ITansuDbContext db,
        ITansuAccessService accessService,
        CancellationToken ct,
        bool writeAccess = false)
    {
        if (currentUser.UserType == UserType.Employee)
        {
            if (currentUser.EmployeeId != employeeId)
                throw new Common.Exceptions.ForbiddenException();
            if (writeAccess)
                throw new Common.Exceptions.ForbiddenException("Сотрудник не может изменять документы через этот API.");
            return;
        }

        if (currentUser.UserType == UserType.Subcontractor)
        {
            var employee = await db.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == employeeId, ct)
                ?? throw new Common.Exceptions.NotFoundException("Employee", employeeId);

            if (currentUser.SubcontractorId != employee.SubcontractorId)
                throw new Common.Exceptions.ForbiddenException("Сотрудник принадлежит другому субподрядчику.");
            return;
        }

        if (currentUser.UserType == UserType.Tansu)
        {
            var access = await accessService.GetAccessAsync(ct);
            accessService.EnsurePermission(access, p => p.CanViewEmployees, "Нет доступа к сотрудникам.");
            if (writeAccess)
                accessService.EnsurePermission(access, p => p.CanUploadDocuments, "Нет права загружать документы.");
            await accessService.EnsureEmployeeVisibleAsync(employeeId, ct);
        }
    }
}

internal static class EmployeeDocumentMapper
{
    private const int ExpiryWarningDays = 14;

    public static EmployeeDocumentDto ToDto(
        Domain.Entities.EmployeeDocument doc,
        DateTimeOffset now,
        IReadOnlySet<Guid> supersededIds)
    {
        var isExpired = doc.ExpiresAt is not null && doc.ExpiresAt <= now;
        var isExpiringSoon = doc.ExpiresAt is not null &&
                             !isExpired &&
                             doc.ExpiresAt <= now.AddDays(ExpiryWarningDays);

        return new EmployeeDocumentDto(
            doc.Id,
            doc.EmployeeId,
            doc.Name,
            doc.DocumentType,
            EmployeeDocumentType.Label(doc.DocumentType),
            doc.FilePath,
            doc.ContentType,
            doc.UploadedAt,
            doc.ExpiresAt,
            doc.UploadedByUserId,
            doc.UploadedBy?.FullName ?? "—",
            isExpired,
            isExpiringSoon,
            doc.SupersedesDocumentId,
            supersededIds.Contains(doc.Id),
            1);
    }

    public static EmployeeBlockRecordDto ToBlockDto(Domain.Entities.EmployeeBlockRecord row) =>
        new(
            row.Id,
            row.EmployeeId,
            row.InitiatedByUserId,
            row.InitiatedBy?.FullName ?? "—",
            row.InitiatorRole,
            RoleLabel(row.InitiatorRole),
            row.ActionType,
            row.Reason,
            row.Status,
            row.CreatedAt);

    private static string? RoleLabel(string? role) =>
        role switch
        {
            null => null,
            _ when TansuRole.IsValid(role) => TansuRole.Label(role),
            _ => ApproverRole.Label(role)
        };
}
