using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public class ProjectSubcontractorConfiguration : IEntityTypeConfiguration<ProjectSubcontractor>
{
    public void Configure(EntityTypeBuilder<ProjectSubcontractor> e)
    {
        e.ToTable("project_subcontractors");
        e.HasKey(x => new { x.ProjectOid, x.SubcontractorId });
        e.Property(x => x.ProjectOid).HasColumnName("project_oid");
        e.Property(x => x.SubcontractorId).HasColumnName("subcontractor_id");
        e.Property(x => x.ActivityType).HasColumnName("activity_type").HasMaxLength(500);
        e.Property(x => x.CompletionPercent).HasColumnName("completion_percent");
        e.Property(x => x.ProgressReportedAt).HasColumnName("progress_reported_at").HasColumnType("timestamptz");
        e.Property(x => x.ProgressReportedByUserId).HasColumnName("progress_reported_by_user_id");
        e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        e.HasOne(x => x.ProgressReportedBy)
            .WithMany()
            .HasForeignKey(x => x.ProgressReportedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        e.HasOne(x => x.Subcontractor)
            .WithMany(s => s.Projects)
            .HasForeignKey(x => x.SubcontractorId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.Project)
            .WithMany()
            .HasForeignKey(x => x.ProjectOid)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
