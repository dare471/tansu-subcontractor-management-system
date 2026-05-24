using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public class ApprovalMatrixEntryConfiguration : IEntityTypeConfiguration<ApprovalMatrixEntry>
{
    public void Configure(EntityTypeBuilder<ApprovalMatrixEntry> e)
    {
        e.ToTable("approval_matrix");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrderNo).HasColumnName("order_no");
        e.Property(x => x.ProjectOid).HasColumnName("project_oid");
        e.Property(x => x.SubcontractorId).HasColumnName("subcontractor_id");
        e.Property(x => x.UserId).HasColumnName("user_id");
        e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        e.HasIndex(x => new { x.ProjectOid, x.SubcontractorId, x.OrderNo }).IsUnique();

        e.HasOne(x => x.Subcontractor)
            .WithMany()
            .HasForeignKey(x => x.SubcontractorId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.Project)
            .WithMany()
            .HasForeignKey(x => x.ProjectOid)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
