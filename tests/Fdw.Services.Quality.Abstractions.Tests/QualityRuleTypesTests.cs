using Fdw.Services.Quality.Abstractions.TypeCollections.QualityRuleTypeOptions;

namespace Fdw.Services.Quality.Abstractions.Tests;

public class QualityRuleTypesTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void AllReturnsAllRuleTypes()
    {
        // Act
        var all = QualityRuleTypes.All();

        // Assert
        all.ShouldNotBeEmpty();
        all.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ByIdReturnsCorrectRuleType()
    {
        // Arrange
        var all = QualityRuleTypes.All();
        var first = all.First();

        // Act
        var result = QualityRuleTypes.ById(first.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(first.Id);
        result.Name.ShouldBe(first.Name);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        // Act
        var result = QualityRuleTypes.ById(99999);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsCorrectRuleType()
    {
        // Act
        var result = QualityRuleTypes.ByName("NotNull");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("NotNull");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Act
        var result = QualityRuleTypes.ByName("UnknownRule");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ByNameIsCaseSensitive()
    {
        // Act
        var result1 = QualityRuleTypes.ByName("NotNull");
        var result2 = QualityRuleTypes.ByName("notnull");
        var result3 = QualityRuleTypes.ByName("NOTNULL");

        // Assert
        result1.Name.ShouldBe("NotNull");
        result2.Name.ShouldBe("_Empty");
        result3.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var result = QualityRuleTypes.NotFound;

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void NotNullRuleTypeHasCorrectProperties()
    {
        // Act
        var rule = QualityRuleTypes.ByName("NotNull");

        // Assert
        rule.ShouldNotBeNull();
        rule.Id.ShouldBe(1);
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
        rule.Id.ShouldBe(2);
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
        rule.Id.ShouldBe(3);
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
        rule.Id.ShouldBe(4);
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
        rule.Id.ShouldBe(5);
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
        rule.Id.ShouldBe(10);
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
        rule.Id.ShouldBe(12);
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
        rule.Id.ShouldBe(11);
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
        rule.Id.ShouldBe(6);
        rule.Name.ShouldBe("CustomExpression");
        rule.RequiresField.ShouldBeTrue();
        rule.SupportsMultipleFields.ShouldBeFalse();
        rule.RequiresParameters.ShouldBeTrue();
        rule.Description.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void AllRuleTypesHaveUniqueIds()
    {
        // Act
        var all = QualityRuleTypes.All();
        var ids = all.Select(r => r.Id).ToList();

        // Assert
        ids.Count.ShouldBe(ids.Distinct().Count());
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void AllRuleTypesHaveUniqueNames()
    {
        // Act
        var all = QualityRuleTypes.All();
        var names = all.Select(r => r.Name).ToList();

        // Assert
        names.Count.ShouldBe(names.Distinct().Count());
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void AllRuleTypesImplementInterface()
    {
        // Act
        var all = QualityRuleTypes.All();

        // Assert
        foreach (var rule in all)
        {
            rule.ShouldBeAssignableTo<IQualityRuleType>();
        }
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void AllRuleTypesHaveNonEmptyDescription()
    {
        // Act
        var all = QualityRuleTypes.All();

        // Assert
        foreach (var rule in all)
        {
            rule.Description.ShouldNotBeNullOrEmpty();
        }
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
