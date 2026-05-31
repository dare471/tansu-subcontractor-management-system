using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public sealed class EmployeePhotoReviewConfiguration : IEntityTypeConfiguration<EmployeePhotoReview>
{
    public void Configure(EntityTypeBuilder<EmployeePhotoReview> e)
    {
        e.ToTable("employee_photo_reviews");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.EmployeeId).HasColumnName("employee_id");
        e.Property(x => x.PhotoPath).HasColumnName("photo_path").HasMaxLength(1024);
        e.Property(x => x.ReviewType).HasColumnName("review_type").HasMaxLength(16);
        e.Property(x => x.Result).HasColumnName("result").HasMaxLength(16);
        e.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(2000);
        e.Property(x => x.DetailsJson).HasColumnName("details_json");
        e.Property(x => x.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
        e.Property(x => x.CreatedAt).HasColumnName("created_at");

        e.HasIndex(x => new { x.EmployeeId, x.CreatedAt });

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.ReviewedBy)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
