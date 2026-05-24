using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public class EmployeeApprovalBatchMemberConfiguration : IEntityTypeConfiguration<EmployeeApprovalBatchMember>
{
    public void Configure(EntityTypeBuilder<EmployeeApprovalBatchMember> e)
    {
        e.ToTable("employee_approval_batch_members");
        e.HasKey(x => new { x.BatchId, x.EmployeeId });
        e.Property(x => x.BatchId).HasColumnName("batch_id");
        e.Property(x => x.EmployeeId).HasColumnName("employee_id");
        e.Property(x => x.AddedAt).HasColumnName("added_at").HasColumnType("timestamptz");

        e.HasIndex(x => x.EmployeeId);

        e.HasOne(x => x.Batch)
            .WithMany(b => b.Members)
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
