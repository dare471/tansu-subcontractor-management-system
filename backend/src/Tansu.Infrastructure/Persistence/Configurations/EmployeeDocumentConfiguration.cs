using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public sealed class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
{
    public void Configure(EntityTypeBuilder<EmployeeDocument> e)
    {
        e.ToTable("employee_documents");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.EmployeeId).HasColumnName("employee_id");
        e.Property(x => x.Name).HasColumnName("name").HasMaxLength(500);
        e.Property(x => x.DocumentType).HasColumnName("document_type").HasMaxLength(32);
        e.Property(x => x.FilePath).HasColumnName("file_path").HasMaxLength(1024);
        e.Property(x => x.UploadedAt).HasColumnName("uploaded_at");
        e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        e.Property(x => x.UploadedByUserId).HasColumnName("uploaded_by_user_id");
        e.Property(x => x.SupersedesDocumentId).HasColumnName("supersedes_document_id");
        e.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(64);
        e.Property(x => x.ExpiryNotifiedAt).HasColumnName("expiry_notified_at");

        e.HasIndex(x => new { x.EmployeeId, x.UploadedAt });

        e.HasOne(x => x.SupersedesDocument)
            .WithMany()
            .HasForeignKey(x => x.SupersedesDocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.UploadedBy)
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
