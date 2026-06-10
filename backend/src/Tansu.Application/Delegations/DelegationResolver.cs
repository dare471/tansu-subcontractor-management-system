using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;

namespace Tansu.Application.Delegations;

public static class DelegationResolver
{
    public static async Task<Guid> ResolveEffectiveApproverAsync(
        ITansuDbContext db,
        Guid originalApproverId,
        Guid? projectOid,
        Guid? subcontractorId,
        string? approverRole,
        DateTimeOffset at,
        CancellationToken ct)
    {
        var delegation = await db.ApproverDelegations.AsNoTracking()
            .Where(d => d.IsActive &&
                        d.DelegatorUserId == originalApproverId &&
                        d.ValidFrom <= at &&
                        d.ValidTo >= at &&
                        (d.ProjectOid == null || d.ProjectOid == projectOid) &&
                        (d.SubcontractorId == null || d.SubcontractorId == subcontractorId) &&
                        (d.ApproverRole == null || d.ApproverRole == approverRole))
            .OrderByDescending(d => d.ProjectOid != null)
            .ThenByDescending(d => d.SubcontractorId != null)
            .FirstOrDefaultAsync(ct);

        return delegation?.DelegateUserId ?? originalApproverId;
    }

    public static async Task ApplyToEmployeeSheetAsync(
        ITansuDbContext db,
        ApprovalSheetEntry sheet,
        Employee employee,
        CancellationToken ct)
    {
        var original = sheet.ApproverUserId;
        var effective = await ResolveEffectiveApproverAsync(
            db, original, employee.ProjectOid, employee.SubcontractorId, null, DateTimeOffset.UtcNow, ct);
        if (effective != original)
        {
            sheet.ActingForUserId = original;
            sheet.ApproverUserId = effective;
        }
        sheet.AssignedAt ??= DateTimeOffset.UtcNow;
    }

    public static async Task ApplyToDocumentSheetAsync(
        ITansuDbContext db,
        DocumentApprovalSheetEntry sheet,
        DocumentRequest request,
        CancellationToken ct)
    {
        var original = sheet.ApproverUserId;
        var effective = await ResolveEffectiveApproverAsync(
            db, original, request.ProjectOid, request.SubcontractorId, sheet.ApproverRole,
            DateTimeOffset.UtcNow, ct);
        if (effective != original)
        {
            sheet.ActingForUserId = original;
            sheet.ApproverUserId = effective;
        }
        sheet.AssignedAt ??= DateTimeOffset.UtcNow;
    }
}
