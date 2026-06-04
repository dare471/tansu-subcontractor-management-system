namespace Tansu.Domain.Entities;

public class EmployeeSafetyQuizCompletion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public DateTimeOffset CompletedAt { get; set; } = DateTimeOffset.UtcNow;

    public Employee? Employee { get; set; }
}
