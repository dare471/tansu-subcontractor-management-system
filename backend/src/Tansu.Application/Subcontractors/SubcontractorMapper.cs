using Tansu.Domain.Entities;

namespace Tansu.Application.Subcontractors;

internal static class SubcontractorMapper
{
    public static SubcontractorDto ToDto(
        Subcontractor entity,
        int projectsCount,
        int approved,
        int notApproved,
        string? managerFullName = null) =>
        new(
            entity.Id,
            entity.Name,
            entity.Bin,
            projectsCount,
            approved,
            notApproved,
            entity.IsActive,
            entity.ManagerUserId,
            managerFullName,
            entity.CreatedAt);
}
