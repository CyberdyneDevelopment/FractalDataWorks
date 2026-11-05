using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Quality.Thresholds;

namespace Fdw.Services.Quality.Tests.Thresholds;

/// <summary>Tests for the measures the quality threshold types take (FDW-3935): the rule logic of the quality rule evaluator, as measures a threshold then judges.</summary>
public sealed class QualityMeasuresTests
{
    private static IReadOnlyDictionary<string, object?> Row(object? age, object? name = null) => new Dictionary<string, object?> { ["Age"] = age, ["Name"] = name };

    private static QualityMeasureInput Input(IReadOnlyDictionary<string, object?> parameters, params IReadOnlyDictionary<string, object?>[] rows) =>
        new("T", parameters, rows, TimeSpan.FromSeconds(1));

    private static IReadOnlyDictionary<string, object?> Parameters(params (string Name, object? Value)[] pairs) => pairs.ToDictionary(pair => pair.Name, pair => pair.Value);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void NotNullCountsMissingNullAndBlankValues()
    {
        var measured = QualityMeasures.NotNull(Input(Parameters(("FieldName", "Name")), Row(1, "a"), Row(2, null), Row(3, " "), Row(4, "b")));

        measured.Value!["violations"].ShouldBe(2L);
        measured.Value["rowCount"].ShouldBe(4L);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void UniqueCountsRepeatsAndIgnoresNulls()
    {
        var measured = QualityMeasures.Unique(Input(Parameters(("FieldName", "Name")), Row(1, "a"), Row(2, "a"), Row(3, "b"), Row(4, null)));

        measured.Value!["violations"].ShouldBe(1L);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void InRangeCountsRowsOutsideTheLimitsStatedAndNonNumbers()
    {
        var measured = QualityMeasures.InRange(Input(Parameters(("FieldName", "Age"), ("MinValue", "10"), ("MaxValue", "20")), Row(5), Row(10), Row(20), Row(21), Row("x"), Row(null)));

        measured.Value!["violations"].ShouldBe(4L);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void InRangeWithNoLimitOrANonNumericLimitCannotBeMeasured()
    {
        QualityMeasures.InRange(Input(Parameters(("FieldName", "Age")), Row(1))).IsFailure.ShouldBeTrue();
        QualityMeasures.InRange(Input(Parameters(("FieldName", "Age"), ("MinValue", "low")), Row(1))).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void MatchesPatternCountsRowsThatDoNotMatchAndRefusesAnInvalidPattern()
    {
        var measured = QualityMeasures.MatchesPattern(Input(Parameters(("FieldName", "Name"), ("Pattern", "^[a-z]+$")), Row(1, "abc"), Row(2, "ab1"), Row(3, null), Row(4, "")));

        measured.Value!["violations"].ShouldBe(3L);
        QualityMeasures.MatchesPattern(Input(Parameters(("FieldName", "Name"), ("Pattern", "(")), Row(1, "a"))).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void InReferenceSetCountsValuesOutsideTheSet()
    {
        var allowed = new List<object?> { "a", "b" };

        var measured = QualityMeasures.InReferenceSet(Input(Parameters(("FieldName", "Name"), ("AllowedValues", allowed)), Row(1, "a"), Row(2, "c"), Row(3, null)));

        measured.Value!["violations"].ShouldBe(2L);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void DistinctRowAndNullShareMeasuresAreTaken()
    {
        var rows = new[] { Row(1, "a"), Row(2, "a"), Row(3, null), Row(4, "") };

        QualityMeasures.DistinctCount(Input(Parameters(("FieldName", "Name")), rows)).Value!["distinctCount"].ShouldBe(2L);
        QualityMeasures.RowCount(Input(Parameters(), rows)).Value!["rowCount"].ShouldBe(4L);
        QualityMeasures.NullPercentage(Input(Parameters(("FieldName", "Name")), rows)).Value!["nullPercentage"].ShouldBe(50d);
        QualityMeasures.NullPercentage(Input(Parameters(("FieldName", "Name")))).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AFieldRuleWithoutAFieldNameAndACustomExpressionCannotBeMeasured()
    {
        QualityMeasures.NotNull(Input(Parameters(), Row(1))).IsFailure.ShouldBeTrue();
        QualityMeasures.CustomExpression(Input(Parameters(("Expression", "Age > 1")), Row(1))).IsFailure.ShouldBeTrue();
    }
}
