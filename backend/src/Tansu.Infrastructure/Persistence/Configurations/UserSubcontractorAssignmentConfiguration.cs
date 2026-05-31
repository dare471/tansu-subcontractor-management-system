using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public sealed class UserSubcontractorAssignmentConfiguration : IEntityTypeConfiguration<UserSubcontractorAssignment>
{
    public void Configure(EntityTypeBuilder<UserSubcontractorAssignment> e)
    {
        e.ToTable("user_subcontractor_assignments");
        e.HasKey(x => new { x.UserId, x.SubcontractorId });
        e.Property(x => x.UserId).HasColumnName("user_id");
        e.Property(x => x.SubcontractorId).HasColumnName("subcontractor_id");

        e.HasOne(x => x.User)
            .WithMany(u => u.SubcontractorAssignments)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.Subcontractor)
            .WithMany()
            .HasForeignKey(x => x.SubcontractorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
