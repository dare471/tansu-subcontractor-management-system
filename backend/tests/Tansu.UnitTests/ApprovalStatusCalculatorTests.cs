using FluentAssertions;
using Tansu.Application.Approvals;
using Tansu.Domain.Enums;

namespace Tansu.UnitTests;

public class ApprovalStatusCalculatorTests
{
    [Fact]
    public void Empty_list_returns_draft()
    {
        ApprovalStatusCalculator.DetermineRoundStatus(Array.Empty<string>())
            .Should().Be("draft");
    }

    [Fact]
    public void Any_rejected_returns_rejected()
    {
        var statuses = new[] { ApprovalStatus.Approved, ApprovalStatus.Rejected, ApprovalStatus.Skipped };
        ApprovalStatusCalculator.DetermineRoundStatus(statuses).Should().Be(ApprovalStatus.Rejected);
    }

    [Fact]
    public void Any_pending_returns_pending()
    {
        var statuses = new[] { ApprovalStatus.Approved, ApprovalStatus.Pending };
        ApprovalStatusCalculator.DetermineRoundStatus(statuses).Should().Be(ApprovalStatus.Pending);
    }

    [Fact]
    public void All_approved_returns_approved()
    {
        var statuses = new[] { ApprovalStatus.Approved, ApprovalStatus.Approved };
        ApprovalStatusCalculator.DetermineRoundStatus(statuses).Should().Be(ApprovalStatus.Approved);
    }

    [Fact]
    public void Rejected_dominates_over_pending()
    {
        var statuses = new[] { ApprovalStatus.Approved, ApprovalStatus.Rejected, ApprovalStatus.Pending };
        ApprovalStatusCalculator.DetermineRoundStatus(statuses).Should().Be(ApprovalStatus.Rejected);
    }
}
