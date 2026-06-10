using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;

namespace Tansu.Application.Zup;

public static class ZupProjectSync
{
    public static async Task<int> SyncToLocalRefsAsync(
        ITansuDbContext db,
        IZupProjectDirectory directory,
        CancellationToken ct)
    {
        var remote = await directory.ListAsync(ct);
        if (remote.Count == 0)
            return 0;

        var oids = remote.Select(p => p.ProjectOid).ToList();
        var existing = await db.ProjectRefs
            .Where(p => oids.Contains(p.ProjectOid))
            .ToDictionaryAsync(p => p.ProjectOid, ct);

        var syncedAt = DateTimeOffset.UtcNow;
        foreach (var project in remote)
        {
            if (existing.TryGetValue(project.ProjectOid, out var local))
                ApplyZupFields(local, project, syncedAt);
            else
            {
                var created = new ProjectRef { ProjectOid = project.ProjectOid };
                ApplyZupFields(created, project, syncedAt);
                db.ProjectRefs.Add(created);
            }
        }

        await db.SaveChangesAsync(ct);
        return remote.Count;
    }

    internal static void ApplyZupFields(ProjectRef local, ZupProjectDto remote, DateTimeOffset syncedAt)
    {
        local.ZupId = remote.ZupId;
        local.Code = remote.Code;
        local.Name = remote.Name;
        local.Description = remote.Description;
        local.Address = remote.Address;
        local.Latitude = remote.Latitude;
        local.Longitude = remote.Longitude;
        local.ZupProjectManagerName = remote.ProjectManagerName;
        local.ContractType = remote.ContractType;
        local.ZupSyncedAt = syncedAt;

        if (!string.IsNullOrWhiteSpace(remote.CustomerName))
            local.CustomerName = remote.CustomerName;
    }
}
