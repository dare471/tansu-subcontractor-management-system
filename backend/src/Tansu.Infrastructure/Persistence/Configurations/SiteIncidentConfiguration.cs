using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public sealed class SiteIncidentConfiguration : IEntityTypeConfiguration<SiteIncident>
{
    public void Configure(EntityTypeBuilder<SiteIncident> e)
    {
        e.ToTable("site_incidents");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.ProjectOid).HasColumnName("project_oid");
        e.Property(x => x.OccurredAt).HasColumnName("occurred_at");
        e.Property(x => x.ReportedByUserId).HasColumnName("reported_by_user_id");
        e.Property(x => x.Title).HasColumnName("title").HasMaxLength(500);
        e.Property(x => x.Description).HasColumnName("description").HasMaxLength(4000);
        e.Property(x => x.Severity).HasColumnName("severity").HasMaxLength(16);
        e.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
        e.Property(x => x.SubcontractorId).HasColumnName("subcontractor_id");
        e.Property(x => x.BlockUntilResolved).HasColumnName("block_until_resolved");
        e.Property(x => x.ResolutionNotes).HasColumnName("resolution_notes").HasMaxLength(4000);
        e.Property(x => x.ResolvedAt).HasColumnName("resolved_at");
        e.Property(x => x.ResolvedByUserId).HasColumnName("resolved_by_user_id");
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectOid).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.ReportedBy).WithMany().HasForeignKey(x => x.ReportedByUserId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Subcontractor).WithMany().HasForeignKey(x => x.SubcontractorId).OnDelete(DeleteBehavior.SetNull);
        e.HasIndex(x => x.ProjectOid);
        e.HasIndex(x => x.Status);
    }
}

public sealed class SiteIncidentEmployeeConfiguration : IEntityTypeConfiguration<SiteIncidentEmployee>
{
    public void Configure(EntityTypeBuilder<SiteIncidentEmployee> e)
    {
        e.ToTable("site_incident_employees");
        e.HasKey(x => new { x.IncidentId, x.EmployeeId });
        e.Property(x => x.IncidentId).HasColumnName("incident_id");
        e.Property(x => x.EmployeeId).HasColumnName("employee_id");
        e.HasOne(x => x.Incident).WithMany(i => i.LinkedEmployees).HasForeignKey(x => x.IncidentId);
        e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId);
    }
}

public sealed class SiteIncidentCommentConfiguration : IEntityTypeConfiguration<SiteIncidentComment>
{
    public void Configure(EntityTypeBuilder<SiteIncidentComment> e)
    {
        e.ToTable("site_incident_comments");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.IncidentId).HasColumnName("incident_id");
        e.Property(x => x.AuthorUserId).HasColumnName("author_user_id");
        e.Property(x => x.Body).HasColumnName("body").HasMaxLength(4000);
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.HasOne(x => x.Incident).WithMany(i => i.Comments).HasForeignKey(x => x.IncidentId);
        e.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
