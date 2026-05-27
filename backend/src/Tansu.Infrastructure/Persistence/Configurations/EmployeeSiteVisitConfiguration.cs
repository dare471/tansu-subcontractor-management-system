using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public sealed class EmployeeSiteVisitConfiguration : IEntityTypeConfiguration<EmployeeSiteVisit>
{
    public void Configure(EntityTypeBuilder<EmployeeSiteVisit> e)
    {
        e.ToTable("employee_site_visits");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.EmployeeId).HasColumnName("employee_id");
        e.Property(x => x.AccessPassId).HasColumnName("access_pass_id");
        e.Property(x => x.CheckedInAt).HasColumnName("checked_in_at");
        e.Property(x => x.FaceConfidence).HasColumnName("face_confidence");
        e.Property(x => x.VerificationMethod).HasColumnName("verification_method").HasMaxLength(32);

        e.HasIndex(x => new { x.EmployeeId, x.CheckedInAt });

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.AccessPass)
            .WithMany()
            .HasForeignKey(x => x.AccessPassId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
