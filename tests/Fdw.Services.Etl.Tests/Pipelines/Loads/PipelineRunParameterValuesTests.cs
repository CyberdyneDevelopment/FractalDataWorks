using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>
/// Tests for <see cref="PipelineRunParameterValues"/> (FDW-1109): what a trigger or schedule may supply, read as the
/// declared type, with every refusal naming the parameter.
/// </summary>
public sealed class PipelineRunParameterValuesTests
{
    private static PipelineRunParameterConfiguration Declared(string name, string type, bool required = false) =>
        new() { Name = name, ParameterType = type, IsRequired = required };

    private static List<KeyValuePair<string, string>> Supplied(params (string Name, string Value)[] pairs) =>
        pairs.Select(p => new KeyValuePair<string, string>(p.Name, p.Value)).ToList();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ResolveReadsEachValueAsItsDeclaredType()
    {
        // Arrange
        IList<PipelineRunParameterConfiguration> declared =
        [
            Declared("Region", "String"), Declared("Limit", "Int64"), Declared("Rate", "Decimal"), Declared("Since", "DateTimeOffset"), Declared("Full", "Bool"),
        ];
        var supplied = Supplied(("Region", "EU"), ("Limit", "10"), ("Rate", "0.25"), ("Since", "2026-01-01T00:00:00Z"), ("Full", "true"));

        // Act
        var result = PipelineRunParameterValues.Resolve(declared, supplied, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!["Region"].ShouldBe("EU");
        result.Value["Limit"].ShouldBe(10L);
        result.Value["Rate"].ShouldBe(0.25m);
        result.Value["Since"].ShouldBe(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        result.Value["Full"].ShouldBe(true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ResolveMatchesNamesIgnoringCase()
    {
        // Act
        var result = PipelineRunParameterValues.Resolve([Declared("Region", "String")], Supplied(("region", "EU")), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!["REGION"].ShouldBe("EU");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ResolveRefusesAnUndeclaredName()
    {
        // Act
        var result = PipelineRunParameterValues.Resolve([Declared("Region", "String")], Supplied(("Other", "x")), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20201");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Other");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ResolveRefusesAnyNameWhenThePipelineDeclaresNone()
    {
        // Act
        var result = PipelineRunParameterValues.Resolve(null, Supplied(("Region", "EU")), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20201");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ResolveRefusesAParameterSuppliedTwice()
    {
        // Act
        var result = PipelineRunParameterValues.Resolve(
            [Declared("Region", "String")], Supplied(("Region", "EU"), ("region", "US")), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20202");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ResolveRefusesAMissingRequiredParameterAndNamesIt()
    {
        // Act
        var result = PipelineRunParameterValues.Resolve(
            [Declared("Region", "String", required: true), Declared("Limit", "Int64")], Supplied(("Limit", "5")), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20203");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Region");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ResolveLeavesAnAbsentOptionalParameterAbsent()
    {
        // Act
        var result = PipelineRunParameterValues.Resolve([Declared("Limit", "Int64")], [], NullLogger.Instance);

        // Assert: no default is invented
        result.IsSuccess.ShouldBeTrue();
        result.Value!.ContainsKey("Limit").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ResolveRefusesATextThatIsNotAValueOfTheDeclaredType()
    {
        // Act
        var result = PipelineRunParameterValues.Resolve([Declared("Limit", "Int64")], Supplied(("Limit", "ten")), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20204");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Limit");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Int64");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ResolveAcceptsTheReservedWatermarkAsADateTimeOffsetOverride()
    {
        // Act
        var result = PipelineRunParameterValues.Resolve(null, Supplied(("LastSuccessfulRun", "2026-05-01T00:00:00Z")), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value![PipelineRunParameterValues.LastSuccessfulRun].ShouldBe(new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ResolveRefusesAWatermarkOverrideThatIsNotADateTimeOffset()
    {
        // Act
        var result = PipelineRunParameterValues.Resolve(null, Supplied(("LastSuccessfulRun", "yesterday")), NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20204");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void VerifyAcceptsTypedValuesOfTheDeclaredTypes()
    {
        // Arrange
        var values = new Dictionary<string, object?> { ["Limit"] = 10L, ["Region"] = "EU" };

        // Act
        var result = PipelineRunParameterValues.Verify([Declared("Limit", "Int64"), Declared("Region", "String", required: true)], values, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void VerifyRefusesAValueOfTheWrongClrType()
    {
        // Arrange: an int where the declared Int is a long
        var values = new Dictionary<string, object?> { ["Limit"] = 10 };

        // Act
        var result = PipelineRunParameterValues.Verify([Declared("Limit", "Int64")], values, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20204");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void VerifyRefusesAnUndeclaredNameAndANullValue()
    {
        // Act
        var undeclared = PipelineRunParameterValues.Verify([Declared("Limit", "Int64")], new Dictionary<string, object?> { ["Other"] = 1L }, NullLogger.Instance);
        var nullValue = PipelineRunParameterValues.Verify([Declared("Limit", "Int64")], new Dictionary<string, object?> { ["Limit"] = null }, NullLogger.Instance);

        // Assert
        undeclared.Messages[^1].Code.ShouldBe("PIPELINERUN-20201");
        nullValue.Messages[^1].Code.ShouldBe("PIPELINERUN-20204");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void VerifyRefusesAMissingRequiredParameterWhenNothingWasSupplied()
    {
        // Act
        var result = PipelineRunParameterValues.Verify([Declared("Region", "String", required: true)], null, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20203");
    }
}
