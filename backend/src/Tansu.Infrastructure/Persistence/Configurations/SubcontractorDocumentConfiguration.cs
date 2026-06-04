using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public sealed class SubcontractorDocumentConfiguration : IEntityTypeConfiguration<SubcontractorDocument>
{
    public void Configure(EntityTypeBuilder<SubcontractorDocument> e)
    {
        e.ToTable("subcontractor_documents");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.SubcontractorId).HasColumnName("subcontractor_id");
        e.Property(x => x.Name).HasColumnName("name").HasMaxLength(500).IsRequired();
        e.Property(x => x.DocumentType).HasColumnName("document_type").HasMaxLength(32).IsRequired();
        e.Property(x => x.FilePath).HasColumnName("file_path").HasMaxLength(1024).IsRequired();
        e.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(128);
        e.Property(x => x.UploadedAt).HasColumnName("uploaded_at").HasColumnType("timestamptz");
        e.Property(x => x.UploadedByUserId).HasColumnName("uploaded_by_user_id");

        e.HasOne(x => x.Subcontractor)
            .WithMany()
            .HasForeignKey(x => x.SubcontractorId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.UploadedBy)
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
