using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Operations.Abstractions.Thresholds;

namespace Fdw.Services.Quality.Tests.Thresholds;

/// <summary>Tests for the quality threshold types (FDW-3935): every existing rule kind has a type, on a data set, and a type's declarations are well formed.</summary>
public sealed class QualityThresholdTypeTests
{
    private static readonly string[] RuleTypes =
    [
        "NotNull", "Unique", "InRange", "MatchesPattern", "InReferenceSet", "DistinctCountInRange", "RowCountInRange", "NullPercentageBelow", "CustomExpression",
    ];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void EveryQualityRuleKindIsAThresholdTypeOnADataSet()
    {
        foreach (var rule in RuleTypes)
        {
            var type = ThresholdTypes.ByName("Quality" + rule);

            ReferenceEquals(type, ThresholdTypes.NotFound).ShouldBeFalse(rule);
            type.SubjectKind.ShouldBe("DataSet");
            type.Fields.ShouldNotBeEmpty();
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ATypesFieldAndParameterNamesAreUniqueAndAParameterNeverSharesAFieldName()
    {
        foreach (var rule in RuleTypes)
        {
            var type = ThresholdTypes.ByName("Quality" + rule);
            var names = type.Fields.Select(field => field.Name).Concat(type.Parameters.Select(parameter => parameter.Name)).ToList();

            names.Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(names.Count, rule);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheFieldRulesRequireTheFieldTheyReadAndRowCountInRangeNeedsNoParameter()
    {
        foreach (var rule in RuleTypes.Where(rule => rule != "RowCountInRange" && rule != "CustomExpression"))
        {
            ThresholdTypes.ByName("Quality" + rule).Parameters.ShouldContain(parameter => parameter.Name == "FieldName" && parameter.IsRequired, rule);
        }

        ThresholdTypes.ByName("QualityRowCountInRange").Parameters.ShouldBeEmpty();
    }
}
