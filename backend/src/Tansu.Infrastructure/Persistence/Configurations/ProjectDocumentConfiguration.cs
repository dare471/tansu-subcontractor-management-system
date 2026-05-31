using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public sealed class ProjectDocumentConfiguration : IEntityTypeConfiguration<ProjectDocument>
{
    public void Configure(EntityTypeBuilder<ProjectDocument> e)
    {
        e.ToTable("project_documents");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.ProjectOid).HasColumnName("project_oid");
        e.Property(x => x.Name).HasColumnName("name").HasMaxLength(500).IsRequired();
        e.Property(x => x.DocumentType).HasColumnName("document_type").HasMaxLength(32).IsRequired();
        e.Property(x => x.FilePath).HasColumnName("file_path").HasMaxLength(1024).IsRequired();
        e.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(128);
        e.Property(x => x.UploadedAt).HasColumnName("uploaded_at").HasColumnType("timestamptz");
        e.Property(x => x.UploadedByUserId).HasColumnName("uploaded_by_user_id");

        e.HasIndex(x => new { x.ProjectOid, x.UploadedAt });

        e.HasOne(x => x.Project)
            .WithMany(p => p.Documents)
            .HasForeignKey(x => x.ProjectOid)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.UploadedBy)
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
