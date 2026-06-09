using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public sealed class ApprovalSlaPolicyConfiguration : IEntityTypeConfiguration<ApprovalSlaPolicy>
{
    public void Configure(EntityTypeBuilder<ApprovalSlaPolicy> e)
    {
        e.ToTable("approval_sla_policies");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.Scope).HasColumnName("scope").HasMaxLength(32);
        e.Property(x => x.ProjectOid).HasColumnName("project_oid");
        e.Property(x => x.RequestType).HasColumnName("request_type").HasMaxLength(32);
        e.Property(x => x.PendingDaysWarning).HasColumnName("pending_days_warning");
        e.Property(x => x.PendingDaysEscalation).HasColumnName("pending_days_escalation");
        e.Property(x => x.EscalationRole).HasColumnName("escalation_role").HasMaxLength(32);
        e.Property(x => x.EscalationUserId).HasColumnName("escalation_user_id");
        e.Property(x => x.IsActive).HasColumnName("is_active");
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
    }
}
