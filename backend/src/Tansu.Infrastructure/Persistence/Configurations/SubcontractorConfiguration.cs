using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public class SubcontractorConfiguration : IEntityTypeConfiguration<Subcontractor>
{
    public void Configure(EntityTypeBuilder<Subcontractor> e)
    {
        e.ToTable("subcontractors");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.Name).HasColumnName("name").HasMaxLength(500).IsRequired();
        e.Property(x => x.Bin).HasColumnName("bin").HasMaxLength(32).IsRequired();
        e.Property(x => x.IsActive).HasColumnName("is_active");
        e.Property(x => x.RegisteredByUserId).HasColumnName("registered_by_user_id");
        e.Property(x => x.ManagerUserId).HasColumnName("manager_user_id");
        e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        e.HasIndex(x => x.Bin).IsUnique();

        e.HasOne(x => x.RegisteredBy)
            .WithMany()
            .HasForeignKey(x => x.RegisteredByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        e.HasOne(x => x.Manager)
            .WithMany()
            .HasForeignKey(x => x.ManagerUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
