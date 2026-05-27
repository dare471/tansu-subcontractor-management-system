using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public sealed class EmployeeAccessPassConfiguration : IEntityTypeConfiguration<EmployeeAccessPass>
{
    public void Configure(EntityTypeBuilder<EmployeeAccessPass> e)
    {
        e.ToTable("employee_access_passes");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.EmployeeId).HasColumnName("employee_id");
        e.Property(x => x.Token).HasColumnName("token").HasMaxLength(64);
        e.Property(x => x.IssuedAt).HasColumnName("issued_at");
        e.Property(x => x.RevokedAt).HasColumnName("revoked_at");

        e.HasIndex(x => x.Token).IsUnique();
        e.HasIndex(x => x.EmployeeId);

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
