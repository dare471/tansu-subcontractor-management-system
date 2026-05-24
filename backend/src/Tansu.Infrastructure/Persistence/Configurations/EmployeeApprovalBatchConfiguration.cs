using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Infrastructure.Persistence.Configurations;

public class EmployeeApprovalBatchConfiguration : IEntityTypeConfiguration<EmployeeApprovalBatch>
{
    public void Configure(EntityTypeBuilder<EmployeeApprovalBatch> e)
    {
        e.ToTable("employee_approval_batches", t =>
        {
            t.HasCheckConstraint(
                "ck_employee_approval_batches_status",
                $"status IN ('{BatchStatus.Draft}','{BatchStatus.Submitted}')");
        });
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.SubcontractorId).HasColumnName("subcontractor_id");
        e.Property(x => x.ProjectOid).HasColumnName("project_oid");
        e.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        e.Property(x => x.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        e.Property(x => x.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        e.Property(x => x.EmployeeCount).HasColumnName("employee_count");
        e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        e.Property(x => x.SubmittedAt).HasColumnName("submitted_at").HasColumnType("timestamptz");

        e.HasIndex(x => new { x.SubcontractorId, x.CreatedAt });

        e.HasOne(x => x.Subcontractor).WithMany().HasForeignKey(x => x.SubcontractorId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectOid).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
