using FluentAssertions;
using Tansu.Application.EmployeePortal;

namespace Tansu.UnitTests;

public sealed class SafetyQuizCatalogTests
{
    [Theory]
    [InlineData("en", "Must you use PPE")]
    [InlineData("kk", "ЖҚҚ")]
    [InlineData("ru", "средства индивидуальной защиты")]
    [InlineData(null, "средства индивидуальной защиты")]
    public void GetQuestions_returns_locale_catalog(string? locale, string expectedFragment)
    {
        var questions = SafetyQuizCatalog.GetQuestions(locale);
        questions.Should().HaveCount(5);
        questions[0].Text.Should().Contain(expectedFragment);
    }

    [Fact]
    public void Grade_passes_when_all_answers_correct()
    {
        var answers = new Dictionary<string, string>
        {
            ["q1"] = "a",
            ["q2"] = "b",
            ["q3"] = "b",
            ["q4"] = "b",
            ["q5"] = "a"
        };

        var (score, total, passed) = SafetyQuizCatalog.Grade(answers);
        score.Should().Be(5);
        total.Should().Be(5);
        passed.Should().BeTrue();
    }

    [Fact]
    public void Grade_fails_when_any_answer_wrong()
    {
        var answers = new Dictionary<string, string>
        {
            ["q1"] = "b",
            ["q2"] = "b",
            ["q3"] = "b",
            ["q4"] = "b",
            ["q5"] = "a"
        };

        var (_, _, passed) = SafetyQuizCatalog.Grade(answers);
        passed.Should().BeFalse();
    }
}
