using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public sealed class UserBlockRecordConfiguration : IEntityTypeConfiguration<UserBlockRecord>
{
    public void Configure(EntityTypeBuilder<UserBlockRecord> e)
    {
        e.ToTable("user_block_records");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.UserId).HasColumnName("user_id");
        e.Property(x => x.InitiatedByUserId).HasColumnName("initiated_by_user_id");
        e.Property(x => x.ActionType).HasColumnName("action_type").HasMaxLength(16);
        e.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(1000);
        e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        e.HasIndex(x => new { x.UserId, x.CreatedAt });

        e.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.InitiatedBy)
            .WithMany()
            .HasForeignKey(x => x.InitiatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
