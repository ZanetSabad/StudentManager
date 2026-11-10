using StudentManager.Web.Models;
using StudentManager.Web.Services;

namespace StudentManager.Tests.Services;

/// <summary>
/// Testy pravidla, jaký stav dostane student podle username (docs/ROZHODNUTI.md).
/// </summary>
public class StudentStatusEvaluatorTests
{
    private readonly StudentStatusEvaluator _evaluator = new();

    /// <summary>Admin převzal návrh bez problému → Koncept.</summary>
    [Fact]
    public void Evaluate_AcceptedSuggestionWithoutReview_ReturnsDraft()
    {
        var suggestion = new UsernameResult("Novak.Jan", false, null);

        var decision = _evaluator.Evaluate("Novak.Jan", suggestion);

        Assert.Equal(StudentStatus.Draft, decision.Status);
        Assert.Null(decision.ReviewReason);
    }

    /// <summary>Admin převzal návrh, který chce kontrolu → Ke kontrole s důvodem od generátoru.</summary>
    [Fact]
    public void Evaluate_AcceptedSuggestionWithReview_ReturnsNeedsReviewWithGeneratorReason()
    {
        var suggestion = new UsernameResult("Novak.Jan2", true, "Kolize: Novak.Jan je obsazené, přiděleno Novak.Jan2.");

        var decision = _evaluator.Evaluate("Novak.Jan2", suggestion);

        Assert.Equal(StudentStatus.NeedsReview, decision.Status);
        Assert.Equal(suggestion.ReviewReason, decision.ReviewReason);
    }

    /// <summary>Ručně přepsané username → Koncept, i když návrh chtěl kontrolu.</summary>
    [Fact]
    public void Evaluate_ManualUsername_ReturnsDraft()
    {
        var suggestion = new UsernameResult("Novak.Jan2", true, "Kolize…");

        var decision = _evaluator.Evaluate("Novak.Jan.B", suggestion);

        Assert.Equal(StudentStatus.Draft, decision.Status);
        Assert.Null(decision.ReviewReason);
    }

    /// <summary>Jiná velikost písmen než návrh = ruční úprava.</summary>
    [Fact]
    public void Evaluate_DifferentCaseOnly_IsManual()
    {
        var suggestion = new UsernameResult("Novak.Jan2", true, "Kolize…");

        var decision = _evaluator.Evaluate("novak.jan2", suggestion);

        Assert.Equal(StudentStatus.Draft, decision.Status);
    }

    /// <summary>Ručně přepsané a delší než 20 znaků → Ke kontrole.</summary>
    [Fact]
    public void Evaluate_ManualLongerThan20_ReturnsNeedsReview()
    {
        var suggestion = new UsernameResult("Kowalczykowa.Kristina", true, "Delší…");

        var decision = _evaluator.Evaluate("Kowalczykowa.Kristinka", suggestion); // 22 znaků

        Assert.Equal(StudentStatus.NeedsReview, decision.Status);
        Assert.Contains("20", decision.ReviewReason);
    }

    /// <summary>Ručně přepsané a přesně 20 znaků → ještě v pořádku.</summary>
    [Fact]
    public void Evaluate_ManualExactly20_ReturnsDraft()
    {
        var suggestion = new UsernameResult("Novak.Jan", false, null);

        var decision = _evaluator.Evaluate("Kowalczykowa.Jolanta", suggestion); // 20 znaků

        Assert.Equal(StudentStatus.Draft, decision.Status);
    }
}
