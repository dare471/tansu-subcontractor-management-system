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
        e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

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
