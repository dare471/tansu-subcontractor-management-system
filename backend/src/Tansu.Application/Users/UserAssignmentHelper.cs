using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;

namespace Tansu.Application.Users;

internal static class UserAssignmentHelper
{
    public static async Task ValidateAsync(
        ITansuDbContext db,
        IReadOnlyList<Guid>? projectOids,
        IReadOnlyList<Guid>? subcontractorIds,
        CancellationToken ct)
    {
        if (projectOids is { Count: > 0 })
        {
            var distinct = projectOids.Distinct().ToList();
            var found = await db.ProjectRefs.AsNoTracking()
                .Where(p => distinct.Contains(p.ProjectOid))
                .CountAsync(ct);
            if (found != distinct.Count)
                throw new ValidationFailedException("Указан несуществующий проект.");
        }

        if (subcontractorIds is { Count: > 0 })
        {
            var distinct = subcontractorIds.Distinct().ToList();
            var found = await db.Subcontractors.AsNoTracking()
                .Where(s => distinct.Contains(s.Id))
                .CountAsync(ct);
            if (found != distinct.Count)
                throw new ValidationFailedException("Указан несуществующий субподрядчик.");
        }
    }

    public static async Task ReplaceAsync(
        ITansuDbContext db,
        Guid userId,
        IReadOnlyList<Guid>? projectOids,
        IReadOnlyList<Guid>? subcontractorIds,
        CancellationToken ct)
    {
        if (projectOids is not null)
        {
            var existing = await db.UserProjectAssignments
                .Where(a => a.UserId == userId)
                .ToListAsync(ct);
            if (existing.Count > 0)
                db.UserProjectAssignments.RemoveRange(existing);

            foreach (var projectOid in projectOids.Distinct())
            {
                db.UserProjectAssignments.Add(new UserProjectAssignment
                {
                    UserId = userId,
                    ProjectOid = projectOid
                });
            }
        }

        if (subcontractorIds is not null)
        {
            var existing = await db.UserSubcontractorAssignments
                .Where(a => a.UserId == userId)
                .ToListAsync(ct);
            if (existing.Count > 0)
                db.UserSubcontractorAssignments.RemoveRange(existing);

            foreach (var subcontractorId in subcontractorIds.Distinct())
            {
                db.UserSubcontractorAssignments.Add(new UserSubcontractorAssignment
                {
                    UserId = userId,
                    SubcontractorId = subcontractorId
                });
            }
        }
    }
}
