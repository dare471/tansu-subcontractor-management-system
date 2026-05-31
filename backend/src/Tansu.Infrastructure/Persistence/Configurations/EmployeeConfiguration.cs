using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> e)
    {
        e.ToTable("employees");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.SubcontractorId).HasColumnName("subcontractor_id");
        e.Property(x => x.ProjectOid).HasColumnName("project_oid");
        e.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(500).IsRequired();
        e.Property(x => x.Position).HasColumnName("position").HasMaxLength(300).IsRequired();
        e.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(64).IsRequired();
        e.Property(x => x.Iin).HasColumnName("iin").HasMaxLength(32).IsRequired();
        e.Property(x => x.PhotoPath).HasColumnName("photo_path").HasMaxLength(1024);
        e.Property(x => x.PhotoReviewStatus).HasColumnName("photo_review_status").HasMaxLength(16);
        e.Property(x => x.PhotoReviewReason).HasColumnName("photo_review_reason").HasMaxLength(2000);
        e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        e.HasIndex(x => new { x.SubcontractorId, x.ProjectOid });
        e.HasIndex(x => x.Iin);

        e.HasOne(x => x.Subcontractor)
            .WithMany(s => s.Employees)
            .HasForeignKey(x => x.SubcontractorId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.Project)
            .WithMany()
            .HasForeignKey(x => x.ProjectOid)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
