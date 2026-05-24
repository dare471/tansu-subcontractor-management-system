using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public class DocumentRequestMatrixEntryConfiguration : IEntityTypeConfiguration<DocumentRequestMatrixEntry>
{
    public void Configure(EntityTypeBuilder<DocumentRequestMatrixEntry> e)
    {
        e.ToTable("document_approval_matrix");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.ProjectOid).HasColumnName("project_oid");
        e.Property(x => x.SubcontractorId).HasColumnName("subcontractor_id");
        e.Property(x => x.RequestType).HasColumnName("request_type").HasMaxLength(32).IsRequired();
        e.Property(x => x.OrderNo).HasColumnName("order_no");
        e.Property(x => x.ApproverRole).HasColumnName("approver_role").HasMaxLength(32).IsRequired();
        e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        e.HasIndex(x => new { x.ProjectOid, x.SubcontractorId, x.RequestType, x.OrderNo }).IsUnique();

        e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectOid).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.Subcontractor).WithMany().HasForeignKey(x => x.SubcontractorId).OnDelete(DeleteBehavior.Cascade);
    }
}
