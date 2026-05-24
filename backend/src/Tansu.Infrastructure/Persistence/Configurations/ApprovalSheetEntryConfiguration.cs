using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Infrastructure.Persistence.Configurations;

public class ApprovalSheetEntryConfiguration : IEntityTypeConfiguration<ApprovalSheetEntry>
{
    public void Configure(EntityTypeBuilder<ApprovalSheetEntry> e)
    {
        e.ToTable("approval_sheet", t =>
        {
            t.HasCheckConstraint(
                "ck_approval_sheet_status",
                $"status IN ('{ApprovalStatus.Pending}','{ApprovalStatus.Approved}','{ApprovalStatus.Rejected}','{ApprovalStatus.Skipped}')");
        });
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.EmployeeId).HasColumnName("employee_id");
        e.Property(x => x.ApproverUserId).HasColumnName("approver_user_id");
        e.Property(x => x.OrderNo).HasColumnName("order_no");
        e.Property(x => x.RoundId).HasColumnName("round_id");
        e.Property(x => x.BatchId).HasColumnName("batch_id");
        e.Property(x => x.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        e.Property(x => x.DecidedAt).HasColumnName("decided_at").HasColumnType("timestamptz");
        e.Property(x => x.Comment).HasColumnName("comment");
        e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        e.HasIndex(x => new { x.EmployeeId, x.RoundId, x.OrderNo });
        e.HasIndex(x => new { x.ApproverUserId, x.Status });
        e.HasIndex(x => x.BatchId);

        e.HasOne(x => x.Employee)
            .WithMany(emp => emp.ApprovalSheet)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.Approver)
            .WithMany()
            .HasForeignKey(x => x.ApproverUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
