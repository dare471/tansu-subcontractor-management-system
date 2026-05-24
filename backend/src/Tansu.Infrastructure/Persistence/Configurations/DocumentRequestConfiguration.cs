using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public class DocumentRequestConfiguration : IEntityTypeConfiguration<DocumentRequest>
{
    public void Configure(EntityTypeBuilder<DocumentRequest> e)
    {
        e.ToTable("document_requests");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.SubcontractorId).HasColumnName("subcontractor_id");
        e.Property(x => x.ProjectOid).HasColumnName("project_oid");
        e.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        e.Property(x => x.RequestType).HasColumnName("request_type").HasMaxLength(32).IsRequired();
        e.Property(x => x.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        e.Property(x => x.Description).HasColumnName("description").IsRequired();
        e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        e.HasOne(x => x.Subcontractor).WithMany().HasForeignKey(x => x.SubcontractorId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectOid).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
