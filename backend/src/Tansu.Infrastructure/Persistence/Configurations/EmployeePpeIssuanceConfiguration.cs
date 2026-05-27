using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public sealed class EmployeePpeIssuanceConfiguration : IEntityTypeConfiguration<EmployeePpeIssuance>
{
    public void Configure(EntityTypeBuilder<EmployeePpeIssuance> e)
    {
        e.ToTable("employee_ppe_issuances");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.EmployeeId).HasColumnName("employee_id");
        e.Property(x => x.ItemType).HasColumnName("item_type").HasMaxLength(32);
        e.Property(x => x.Size).HasColumnName("size").HasMaxLength(32);
        e.Property(x => x.InventoryNumber).HasColumnName("inventory_number").HasMaxLength(64);
        e.Property(x => x.IssuedAt).HasColumnName("issued_at");
        e.Property(x => x.IssuedByUserId).HasColumnName("issued_by_user_id");
        e.Property(x => x.ReturnedAt).HasColumnName("returned_at");
        e.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(500);

        e.HasIndex(x => new { x.EmployeeId, x.ItemType, x.IssuedAt });

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.IssuedBy)
            .WithMany()
            .HasForeignKey(x => x.IssuedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
