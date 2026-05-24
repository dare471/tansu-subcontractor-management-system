using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public class ProjectRefConfiguration : IEntityTypeConfiguration<ProjectRef>
{
    public void Configure(EntityTypeBuilder<ProjectRef> e)
    {
        e.ToTable("project_refs");
        e.HasKey(x => x.ProjectOid);
        e.Property(x => x.ProjectOid).HasColumnName("project_oid");
        e.Property(x => x.Name).HasColumnName("name").HasMaxLength(500);
        e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
    }
}
