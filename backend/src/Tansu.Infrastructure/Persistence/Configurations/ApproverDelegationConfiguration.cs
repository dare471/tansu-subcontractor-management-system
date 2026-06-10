using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public sealed class ApproverDelegationConfiguration : IEntityTypeConfiguration<ApproverDelegation>
{
    public void Configure(EntityTypeBuilder<ApproverDelegation> e)
    {
        e.ToTable("approver_delegations");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.DelegatorUserId).HasColumnName("delegator_user_id");
        e.Property(x => x.DelegateUserId).HasColumnName("delegate_user_id");
        e.Property(x => x.ProjectOid).HasColumnName("project_oid");
        e.Property(x => x.SubcontractorId).HasColumnName("subcontractor_id");
        e.Property(x => x.ApproverRole).HasColumnName("approver_role").HasMaxLength(32);
        e.Property(x => x.ValidFrom).HasColumnName("valid_from");
        e.Property(x => x.ValidTo).HasColumnName("valid_to");
        e.Property(x => x.IsActive).HasColumnName("is_active");
        e.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.HasOne(x => x.Delegator).WithMany().HasForeignKey(x => x.DelegatorUserId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Delegate).WithMany().HasForeignKey(x => x.DelegateUserId).OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => new { x.DelegatorUserId, x.IsActive });
        e.HasIndex(x => new { x.DelegateUserId, x.IsActive });
    }
}
