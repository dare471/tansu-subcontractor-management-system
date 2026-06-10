using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> e)
    {
        e.ToTable("audit_events");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OccurredAt).HasColumnName("occurred_at");
        e.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        e.Property(x => x.ActorEmail).HasColumnName("actor_email").HasMaxLength(256);
        e.Property(x => x.ActorType).HasColumnName("actor_type").HasMaxLength(32);
        e.Property(x => x.Action).HasColumnName("action").HasMaxLength(64);
        e.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(64);
        e.Property(x => x.EntityId).HasColumnName("entity_id");
        e.Property(x => x.ProjectOid).HasColumnName("project_oid");
        e.Property(x => x.SubcontractorId).HasColumnName("subcontractor_id");
        e.Property(x => x.Summary).HasColumnName("summary").HasMaxLength(1000);
        e.Property(x => x.PayloadJson).HasColumnName("payload_json");
        e.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128);
        e.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
        e.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(512);
        e.HasIndex(x => x.OccurredAt).IsDescending();
        e.HasIndex(x => new { x.EntityType, x.EntityId });
        e.HasIndex(x => x.ActorUserId);
        e.HasIndex(x => x.ProjectOid);
    }
}
