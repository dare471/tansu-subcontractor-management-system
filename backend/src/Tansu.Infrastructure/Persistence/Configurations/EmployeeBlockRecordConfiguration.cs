using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public sealed class EmployeeBlockRecordConfiguration : IEntityTypeConfiguration<EmployeeBlockRecord>
{
    public void Configure(EntityTypeBuilder<EmployeeBlockRecord> e)
    {
        e.ToTable("employee_block_records");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.EmployeeId).HasColumnName("employee_id");
        e.Property(x => x.InitiatedByUserId).HasColumnName("initiated_by_user_id");
        e.Property(x => x.ActionType).HasColumnName("action_type").HasMaxLength(16);
        e.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(1000);
        e.Property(x => x.Status).HasColumnName("status").HasMaxLength(16);
        e.Property(x => x.InitiatorRole).HasColumnName("initiator_role").HasMaxLength(32);
        e.Property(x => x.CreatedAt).HasColumnName("created_at");

        e.HasIndex(x => new { x.EmployeeId, x.CreatedAt });

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.InitiatedBy)
            .WithMany()
            .HasForeignKey(x => x.InitiatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
