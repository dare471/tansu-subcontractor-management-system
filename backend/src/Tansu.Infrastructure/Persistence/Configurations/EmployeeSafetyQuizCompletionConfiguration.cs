using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansu.Domain.Entities;

namespace Tansu.Infrastructure.Persistence.Configurations;

public sealed class EmployeeSafetyQuizCompletionConfiguration
    : IEntityTypeConfiguration<EmployeeSafetyQuizCompletion>
{
    public void Configure(EntityTypeBuilder<EmployeeSafetyQuizCompletion> e)
    {
        e.ToTable("employee_safety_quiz_completions");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.EmployeeId).HasColumnName("employee_id");
        e.Property(x => x.Score).HasColumnName("score");
        e.Property(x => x.TotalQuestions).HasColumnName("total_questions");
        e.Property(x => x.CompletedAt).HasColumnName("completed_at");

        e.HasIndex(x => x.EmployeeId).IsUnique();

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
