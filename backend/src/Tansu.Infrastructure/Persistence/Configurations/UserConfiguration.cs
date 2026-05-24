using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> e)
    {
        e.ToTable("users");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(500).IsRequired();
        e.Property(x => x.Position).HasColumnName("position").HasMaxLength(300).IsRequired();
        e.Property(x => x.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
        e.Property(x => x.PasswordHash).HasColumnName("password_hash");
        e.Property(x => x.UserType).HasColumnName("user_type").HasMaxLength(32).IsRequired();
        e.Property(x => x.SubcontractorId).HasColumnName("subcontractor_id");
        e.Property(x => x.ApproverRole).HasColumnName("approver_role").HasMaxLength(32);
        e.Property(x => x.MustChangePassword).HasColumnName("must_change_password");
        e.Property(x => x.IsActive).HasColumnName("is_active");
        e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        e.HasIndex(x => x.Email).IsUnique();

        e.HasOne(x => x.Subcontractor)
            .WithMany(s => s.Users)
            .HasForeignKey(x => x.SubcontractorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
