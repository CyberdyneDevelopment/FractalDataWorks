using Fdw.Services.Quality.Abstractions.TypeCollections.QualityRuleTypeOptions;

namespace Fdw.Services.Quality.Abstractions.Tests;

public class QualityRuleTypesTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void NotNullRuleTypeHasCorrectProperties()
    {
        // Act
        var rule = QualityRuleTypes.ByName("NotNull");

        // Assert
        rule.ShouldNotBeNull();
        rule.Name.ShouldBe("NotNull");
        rule.RequiresField.ShouldBeTrue();
        rule.SupportsMultipleFields.ShouldBeFalse();
        rule.RequiresParameters.ShouldBeFalse();
        rule.Description.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void UniqueRuleTypeHasCorrectProperties()
    {
        // Act
        var rule = QualityRuleTypes.ByName("Unique");

        // Assert
        rule.ShouldNotBeNull();
        rule.Name.ShouldBe("Unique");
        rule.RequiresField.ShouldBeTrue();
        rule.SupportsMultipleFields.ShouldBeFalse();
        rule.RequiresParameters.ShouldBeFalse();
        rule.Description.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void InRangeRuleTypeHasCorrectProperties()
    {
        // Act
        var rule = QualityRuleTypes.ByName("InRange");

        // Assert
        rule.ShouldNotBeNull();
        rule.Name.ShouldBe("InRange");
        rule.RequiresField.ShouldBeTrue();
        rule.SupportsMultipleFields.ShouldBeFalse();
        rule.RequiresParameters.ShouldBeTrue();
        rule.Description.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void MatchesPatternRuleTypeHasCorrectProperties()
    {
        // Act
        var rule = QualityRuleTypes.ByName("MatchesPattern");

        // Assert
        rule.ShouldNotBeNull();
        rule.Name.ShouldBe("MatchesPattern");
        rule.RequiresField.ShouldBeTrue();
        rule.SupportsMultipleFields.ShouldBeFalse();
        rule.RequiresParameters.ShouldBeTrue();
        rule.Description.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void InReferenceSetRuleTypeHasCorrectProperties()
    {
        // Act
        var rule = QualityRuleTypes.ByName("InReferenceSet");

        // Assert
        rule.ShouldNotBeNull();
        rule.Name.ShouldBe("InReferenceSet");
        rule.RequiresField.ShouldBeTrue();
        rule.SupportsMultipleFields.ShouldBeFalse();
        rule.RequiresParameters.ShouldBeTrue();
        rule.Description.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void RowCountInRangeRuleTypeHasCorrectProperties()
    {
        // Act
        var rule = QualityRuleTypes.ByName("RowCountInRange");

        // Assert
        rule.ShouldNotBeNull();
        rule.Name.ShouldBe("RowCountInRange");
        rule.RequiresField.ShouldBeFalse();
        rule.SupportsMultipleFields.ShouldBeFalse();
        rule.RequiresParameters.ShouldBeTrue();
        rule.Description.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void DistinctCountInRangeRuleTypeHasCorrectProperties()
    {
        // Act
        var rule = QualityRuleTypes.ByName("DistinctCountInRange");

        // Assert
        rule.ShouldNotBeNull();
        rule.Name.ShouldBe("DistinctCountInRange");
        rule.RequiresField.ShouldBeTrue();
        rule.SupportsMultipleFields.ShouldBeFalse();
        rule.RequiresParameters.ShouldBeTrue();
        rule.Description.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void NullPercentageBelowRuleTypeHasCorrectProperties()
    {
        // Act
        var rule = QualityRuleTypes.ByName("NullPercentageBelow");

        // Assert
        rule.ShouldNotBeNull();
        rule.Name.ShouldBe("NullPercentageBelow");
        rule.RequiresField.ShouldBeTrue();
        rule.SupportsMultipleFields.ShouldBeFalse();
        rule.RequiresParameters.ShouldBeTrue();
        rule.Description.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CustomExpressionRuleTypeHasCorrectProperties()
    {
        // Act
        var rule = QualityRuleTypes.ByName("CustomExpression");

        // Assert
        rule.ShouldNotBeNull();
        rule.Name.ShouldBe("CustomExpression");
        rule.RequiresField.ShouldBeTrue();
        rule.SupportsMultipleFields.ShouldBeFalse();
        rule.RequiresParameters.ShouldBeTrue();
        rule.Description.ShouldNotBeNullOrEmpty();
    }

    [Theory]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    [InlineData("NotNull", true, false, false)]
    [InlineData("Unique", true, false, false)]
    [InlineData("InRange", true, false, true)]
    [InlineData("MatchesPattern", true, false, true)]
    [InlineData("InReferenceSet", true, false, true)]
    [InlineData("RowCountInRange", false, false, true)]
    [InlineData("DistinctCountInRange", true, false, true)]
    [InlineData("NullPercentageBelow", true, false, true)]
    [InlineData("CustomExpression", true, false, true)]
    public void RuleTypeHasExpectedRequirements(
        string name,
        bool expectedRequiresField,
        bool expectedSupportsMultipleFields,
        bool expectedRequiresParameters)
    {
        // Act
        var rule = QualityRuleTypes.ByName(name);

        // Assert
        rule.ShouldNotBeNull();
        rule.RequiresField.ShouldBe(expectedRequiresField);
        rule.SupportsMultipleFields.ShouldBe(expectedSupportsMultipleFields);
        rule.RequiresParameters.ShouldBe(expectedRequiresParameters);
    }
}
