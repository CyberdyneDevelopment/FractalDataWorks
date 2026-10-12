using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>
/// Tests for <see cref="LoadFilterExpressions"/> (FDW-1109): the Filter transform's condition language read into the data
/// layer's filter expression, with anything it does not state refused.
/// </summary>
public sealed class LoadFilterExpressionsTests
{
    private static FilterCondition Condition(string text)
    {
        var result = LoadFilterExpressions.Parse("test", text, NullLogger.Instance);
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        return result.Value!.Root.ShouldBeOfType<FilterCondition>();
    }

    private static FilterGroup Group(string text)
    {
        var result = LoadFilterExpressions.Parse("test", text, NullLogger.Instance);
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        return result.Value!.Root.ShouldBeOfType<FilterGroup>();
    }

    [Theory]
    [InlineData("Amount > 5", "Amount", "GreaterThan", 5L)]
    [InlineData("Amount >= 5", "Amount", "GreaterThanOrEqual", 5L)]
    [InlineData("Amount < 5", "Amount", "LessThan", 5L)]
    [InlineData("Amount <= 5", "Amount", "LessThanOrEqual", 5L)]
    [InlineData("Amount == 5", "Amount", "Equal", 5L)]
    [InlineData("Amount != 5", "Amount", "NotEqual", 5L)]
    [InlineData("Name == 'Bob'", "Name", "Equal", "Bob")]
    [InlineData("Name == \"Bob\"", "Name", "Equal", "Bob")]
    [InlineData("Name == Bob", "Name", "Equal", "Bob")]
    [InlineData("Active == true", "Active", "Equal", true)]
    [InlineData("Name contains 'ob'", "Name", "Contains", "ob")]
    [InlineData("Name startswith 'Bo'", "Name", "StartsWith", "Bo")]
    [InlineData("Name endswith 'ob'", "Name", "EndsWith", "ob")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ParseReadsAComparisonIntoAFieldOperatorAndTypedValue(string text, string field, string operatorName, object value)
    {
        // Act
        var condition = Condition(text);

        // Assert
        condition.PropertyName.ShouldBe(field);
        condition.Operator.Name.ShouldBe(operatorName);
        condition.Value.ShouldBe(value);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ParseReadsADecimalAndADateAsTypedValues()
    {
        // Act
        var rate = Condition("Rate >= 1.5");
        var moment = Condition("Modified > 2026-01-01T00:00:00.0000000+00:00");

        // Assert
        rate.Value.ShouldBe(1.5m);
        moment.Value.ShouldBe(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ParseKeepsAQuotedNumberAsText()
    {
        Condition("Code == '007'").Value.ShouldBe("007");
    }

    [Theory]
    [InlineData("Code == null", "IsNull")]
    [InlineData("Code != null", "IsNotNull")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ParseReadsNullChecksWithoutAValue(string text, string operatorName)
    {
        // Act
        var condition = Condition(text);

        // Assert
        condition.PropertyName.ShouldBe("Code");
        condition.Operator.Name.ShouldBe(operatorName);
        condition.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ParseJoinsConditionsWithAnd()
    {
        // Act
        var group = Group("A == 1 AND B == 2 && C == 3");

        // Assert
        group.Operator.Name.ShouldBe("And");
        group.Nodes.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ParseGivesAndHigherPrecedenceThanOr()
    {
        // Act
        var group = Group("A == 1 OR B == 2 AND C == 3");

        // Assert: A, or (B and C)
        group.Operator.Name.ShouldBe("Or");
        group.Nodes.Count.ShouldBe(2);
        group.Nodes[0].ShouldBeOfType<FilterCondition>();
        group.Nodes[1].ShouldBeOfType<FilterGroup>().Operator.Name.ShouldBe("And");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ParseHonoursParentheses()
    {
        // Act
        var group = Group("(A == 1 OR B == 2) AND C == 3");

        // Assert
        group.Operator.Name.ShouldBe("And");
        group.Nodes[0].ShouldBeOfType<FilterGroup>().Operator.Name.ShouldBe("Or");
        group.Nodes[1].ShouldBeOfType<FilterCondition>().PropertyName.ShouldBe("C");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ParseUnwrapsAWholeParenthesisedCondition()
    {
        Condition("(Amount > 5)").PropertyName.ShouldBe("Amount");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ParseDoesNotSplitInsideAQuotedValue()
    {
        // Act
        var condition = Condition("Name == 'a AND b OR c'");

        // Assert
        condition.Value.ShouldBe("a AND b OR c");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Amount")]
    [InlineData("Amount >")]
    [InlineData("== 5")]
    [InlineData("1bad == 2")]
    [InlineData("A b == 2")]
    [InlineData("(A == 1")]
    [InlineData("A == 1 AND  AND B == 2")]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ParseRefusesWhatTheLanguageDoesNotState(string text)
    {
        // Act
        var result = LoadFilterExpressions.Parse("load step 'copy'", text, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20216");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("load step 'copy'");
    }
}
