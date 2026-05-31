using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public class ProjectRefConfiguration : IEntityTypeConfiguration<ProjectRef>
{
    public void Configure(EntityTypeBuilder<ProjectRef> e)
    {
        e.ToTable("project_refs");
        e.HasKey(x => x.ProjectOid);
        e.Property(x => x.ProjectOid).HasColumnName("project_oid");
        e.Property(x => x.Name).HasColumnName("name").HasMaxLength(500);
        e.Property(x => x.CustomerName).HasColumnName("customer_name").HasMaxLength(500);
        e.Property(x => x.CustomerPhone).HasColumnName("customer_phone").HasMaxLength(64);
        e.Property(x => x.CustomerEmail).HasColumnName("customer_email").HasMaxLength(256);
        e.Property(x => x.BudgetAmount).HasColumnName("budget_amount").HasPrecision(18, 2);
        e.Property(x => x.BudgetCurrency).HasColumnName("budget_currency").HasMaxLength(8);
        e.Property(x => x.ResponsibleAdminUserId).HasColumnName("responsible_admin_user_id");
        e.Property(x => x.ProjectManagerUserId).HasColumnName("project_manager_user_id");
        e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        e.HasOne(x => x.ResponsibleAdmin)
            .WithMany()
            .HasForeignKey(x => x.ResponsibleAdminUserId)
            .OnDelete(DeleteBehavior.SetNull);

        e.HasOne(x => x.ProjectManager)
            .WithMany()
            .HasForeignKey(x => x.ProjectManagerUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
