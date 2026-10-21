using Fdw.Services.Quality.Configuration;
using Fdw.Services.Quality.Endpoints;

namespace Fdw.Web.Api.Tests.Endpoints.Quality;

/// <summary>
/// Tests for <see cref="QualityRuleEvaluator"/> (FDW-1363): the one place a quality rule is judged over the rows of a data
/// set. Both execute endpoints call it, so these are the verdicts a caller gets either way.
/// </summary>
public sealed class QualityRuleEvaluatorTests
{
    private static IReadOnlyList<Dictionary<string, object>> Rows(params object?[] values) =>
        values.Select(v => new Dictionary<string, object> { ["Value"] = v! }).ToList();

    private static QualityRuleImplementationConfiguration Rule(string type, string? field = "Value", string? min = null, string? max = null, string? pattern = null) =>
        new() { RuleType = type, FieldName = field, DataSetName = "Teams", MinValue = min, MaxValue = max, Pattern = pattern };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void NotNullPassesWhenEveryRowHasAValue()
    {
        var verdict = QualityRuleEvaluator.Evaluate(Rule("NotNull"), Rows("a", "b", "c"), 3);

        verdict.Status.ShouldBe("Passed");
        verdict.FailureCount.ShouldBe(0);
        verdict.TotalCount.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void NotNullCountsTheRowsThatAreNullOrBlank()
    {
        var verdict = QualityRuleEvaluator.Evaluate(Rule("NotNull"), Rows("a", null, "  ", "d"), 4);

        verdict.Status.ShouldBe("Failed");
        verdict.FailureCount.ShouldBe(2);
        verdict.TotalCount.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void UniqueCountsTheRepeatedValues()
    {
        var verdict = QualityRuleEvaluator.Evaluate(Rule("Unique"), Rows("a", "b", "a", "a"), 4);

        verdict.Status.ShouldBe("Failed");
        verdict.FailureCount.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void InRangeFailsTheRowsOutsideTheBoundsAndTheOnesThatAreNotNumbers()
    {
        var verdict = QualityRuleEvaluator.Evaluate(Rule("InRange", min: "0", max: "10"), Rows(5, 11, -1, "x", 10), 5);

        verdict.Status.ShouldBe("Failed");
        verdict.FailureCount.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MatchesPatternFailsTheRowsThatDoNotMatch()
    {
        var verdict = QualityRuleEvaluator.Evaluate(Rule("MatchesPattern", pattern: "^[A-Z]{3}$"), Rows("BUF", "buf", "MIAMI"), 3);

        verdict.Status.ShouldBe("Failed");
        verdict.FailureCount.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARuleWithoutItsFieldNameCouldNotRunAndSaysWhy()
    {
        var verdict = QualityRuleEvaluator.Evaluate(Rule("NotNull", field: null), Rows("a"), 1);

        verdict.Status.ShouldBe("Failed");
        verdict.FailureCount.ShouldBe(0);
        verdict.ErrorMessage.ShouldNotBeNull().ShouldContain("FieldName");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACustomExpressionIsNotSupportedAndIsNeverPassed()
    {
        var verdict = QualityRuleEvaluator.Evaluate(Rule("CustomExpression"), Rows("a"), 1);

        verdict.Status.ShouldBe("NotSupported");
        verdict.ErrorMessage.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnUnknownRuleTypeIsNamedAndNotPassed()
    {
        var verdict = QualityRuleEvaluator.Evaluate(Rule("Telepathy"), Rows("a"), 1);

        verdict.Status.ShouldBe("Failed");
        verdict.ErrorMessage.ShouldNotBeNull().ShouldContain("Telepathy");
    }
}
