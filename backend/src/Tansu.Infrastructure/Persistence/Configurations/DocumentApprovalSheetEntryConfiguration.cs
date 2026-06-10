using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public class DocumentApprovalSheetEntryConfiguration : IEntityTypeConfiguration<DocumentApprovalSheetEntry>
{
    public void Configure(EntityTypeBuilder<DocumentApprovalSheetEntry> e)
    {
        e.ToTable("document_approval_sheet");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.DocumentRequestId).HasColumnName("document_request_id");
        e.Property(x => x.ApproverUserId).HasColumnName("approver_user_id");
        e.Property(x => x.ApproverRole).HasColumnName("approver_role").HasMaxLength(32).IsRequired();
        e.Property(x => x.OrderNo).HasColumnName("order_no");
        e.Property(x => x.RoundId).HasColumnName("round_id");
        e.Property(x => x.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        e.Property(x => x.DecidedAt).HasColumnName("decided_at").HasColumnType("timestamptz");
        e.Property(x => x.Comment).HasColumnName("comment");
        e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        e.Property(x => x.AssignedAt).HasColumnName("assigned_at").HasColumnType("timestamptz");
        e.Property(x => x.LastReminderAt).HasColumnName("last_reminder_at").HasColumnType("timestamptz");
        e.Property(x => x.EscalatedAt).HasColumnName("escalated_at").HasColumnType("timestamptz");
        e.Property(x => x.ActingForUserId).HasColumnName("acting_for_user_id");

        e.HasOne(x => x.DocumentRequest).WithMany(d => d.ApprovalSheet).HasForeignKey(x => x.DocumentRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.Approver).WithMany().HasForeignKey(x => x.ApproverUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
