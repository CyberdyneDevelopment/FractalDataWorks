using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>
/// Tests for <see cref="PipelineRunParameterReferences"/> (FDW-1109): the <c>{{name}}</c> references in an expression are
/// replaced by the text of the parameter's value, and a reference with no value is refused, never emptied.
/// </summary>
public sealed class PipelineRunParameterReferencesTests
{
    private static Dictionary<string, object?> Values(params (string Name, object? Value)[] values)
    {
        var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in values)
            dictionary[name] = value;
        return dictionary;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void NamesListsEachReferencedParameterOnceInOrder()
    {
        // Act
        var names = PipelineRunParameterReferences.Names("A > {{Low}} AND A < {{ High }} AND B == {{low}}");

        // Assert
        names.ShouldBe(new[] { "Low", "High" });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("A > 5")]
    [InlineData("A > {{ 5 }}")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void NamesFindsNothingWhereThereIsNoReference(string? expression)
    {
        PipelineRunParameterReferences.Names(expression).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ResolveReplacesEveryReferenceWithTheInvariantTextOfItsValue()
    {
        // Arrange
        var values = Values(("Limit", 10L), ("Rate", 0.5m), ("Full", true), ("Since", new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero)), ("Region", "EU"));

        // Act
        var result = PipelineRunParameterReferences.Resolve(
            "test", "N > {{Limit}} AND R == {{Rate}} AND F == {{Full}} AND T >= {{Since}} AND G == {{Region}}", values, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("N > 10 AND R == 0.5 AND F == true AND T >= 2026-01-02T03:04:05.0000000+00:00 AND G == EU");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ResolveLeavesAnExpressionWithoutReferencesUntouched()
    {
        // Act
        var result = PipelineRunParameterReferences.Resolve("test", "A > 5", Values(), NullLogger.Instance);

        // Assert
        result.Value.ShouldBe("A > 5");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ResolveRefusesAReferenceWithNoValueAndNamesIt()
    {
        // Act
        var result = PipelineRunParameterReferences.Resolve("transform 'T'", "A > {{Limit}}", Values(), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20205");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Limit");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ResolveRefusesAReferenceWhoseValueIsNull()
    {
        // Act
        var result = PipelineRunParameterReferences.Resolve("test", "A > {{Limit}}", Values(("Limit", null)), NullLogger.Instance);

        // Assert
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20205");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ResolveRefusesTheWatermarkOfAFirstRunWithItsOwnReason()
    {
        // Act: nothing is stored and nothing overrides it
        var result = PipelineRunParameterReferences.Resolve("transform 'T'", "Modified > {{LastSuccessfulRun}}", Values(), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20206");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ResolveReadsTheWatermarkWhenOneIsHeld()
    {
        // Arrange
        var values = Values(("LastSuccessfulRun", new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)));

        // Act
        var result = PipelineRunParameterReferences.Resolve("test", "Modified > {{LastSuccessfulRun}}", values, NullLogger.Instance);

        // Assert
        result.Value.ShouldBe("Modified > 2026-06-01T00:00:00.0000000+00:00");
    }

    [Theory]
    [InlineData("x' || 1 == 1")]
    [InlineData("a && b")]
    [InlineData("rock and roll")]
    [InlineData("this or that")]
    [InlineData("(group)")]
    [InlineData("a>=b")]
    [InlineData("it's")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ResolveRefusesATextValueThatCouldChangeWhatTheExpressionMeans(string text)
    {
        // Act
        var result = PipelineRunParameterReferences.Resolve("test", "G == {{Region}}", Values(("Region", text)), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20207");
    }

    [Theory]
    [InlineData("EU")]
    [InlineData("north-east 2")]
    [InlineData("a_b.c:d/e@f+g,h")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ResolvePlacesATextValueThatHoldsOnlyPlaceableCharacters(string text)
    {
        // Act
        var result = PipelineRunParameterReferences.Resolve("test", "G == {{Region}}", Values(("Region", text)), NullLogger.Instance);

        // Assert
        result.Value.ShouldBe($"G == {text}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ResolveRefusesAValueWhoseTypeIsNotOneARunParameterCanHave()
    {
        // Act
        var result = PipelineRunParameterReferences.Resolve("test", "A > {{Odd}}", Values(("Odd", new object())), NullLogger.Instance);

        // Assert
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20207");
    }
}
