using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public sealed class UserProjectAssignmentConfiguration : IEntityTypeConfiguration<UserProjectAssignment>
{
    public void Configure(EntityTypeBuilder<UserProjectAssignment> e)
    {
        e.ToTable("user_project_assignments");
        e.HasKey(x => new { x.UserId, x.ProjectOid });
        e.Property(x => x.UserId).HasColumnName("user_id");
        e.Property(x => x.ProjectOid).HasColumnName("project_oid");

        e.HasOne(x => x.User)
            .WithMany(u => u.ProjectAssignments)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.Project)
            .WithMany()
            .HasForeignKey(x => x.ProjectOid)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
