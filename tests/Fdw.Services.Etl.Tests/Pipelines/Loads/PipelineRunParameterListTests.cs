using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>
/// Tests for the list parameters of <see cref="PipelineRunParameterValues"/> (FDW-1745): a run supplies a JSON array, every item is read
/// as the declared item type, and anything else is refused naming the parameter.
/// </summary>
public sealed class PipelineRunParameterListTests
{
    private static PipelineParameterConfiguration List(string name, string itemType) =>
        new() { Name = name, ParameterType = itemType, IsList = true };

    private static List<KeyValuePair<string, string>> Supplied(string name, string value) => [new(name, value)];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AListOfTextIsReadItemByItemInOrder()
    {
        var result = PipelineRunParameterValues.Resolve([List("Columns", "String")], Supplied("Columns", "[\"Name\",\"City\"]"), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        var items = result.Value!["Columns"].ShouldBeAssignableTo<IReadOnlyList<object>>()!;
        items.ShouldBe(["Name", "City"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AListOfNumbersIsReadAsTheItemType()
    {
        var result = PipelineRunParameterValues.Resolve([List("Sizes", "Int32")], Supplied("Sizes", "[10, 20]"), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        result.Value!["Sizes"].ShouldBeAssignableTo<IReadOnlyList<object>>()!.ShouldBe([10, 20]);
    }

    [Theory]
    [InlineData("Name")]
    [InlineData("\"Name\"")]
    [InlineData("{\"a\":1}")]
    [InlineData("[]")]
    [InlineData("[\"Name\",")]
    [InlineData("")]
    public void ATextThatIsNotAnArrayWithItemsIsRefusedNamingTheParameter(string text)
    {
        var result = PipelineRunParameterValues.Resolve([List("Columns", "String")], Supplied("Columns", text), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AnItemThatIsNotOfTheItemTypeRefusesTheWholeList()
    {
        var result = PipelineRunParameterValues.Resolve([List("Sizes", "Int32")], Supplied("Sizes", "[10, \"many\"]"), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void VerifyTakesATypedListOfTheItemTypeAndRefusesASingleValueOrAWrongItem()
    {
        PipelineParameterConfiguration[] declared = [List("Columns", "String")];

        PipelineRunParameterValues.Verify(declared, new Dictionary<string, object?> { ["Columns"] = new List<object> { "Name", "City" } }, NullLogger.Instance)
            .IsSuccess.ShouldBeTrue();
        PipelineRunParameterValues.Verify(declared, new Dictionary<string, object?> { ["Columns"] = "Name" }, NullLogger.Instance)
            .IsFailure.ShouldBeTrue();
        PipelineRunParameterValues.Verify(declared, new Dictionary<string, object?> { ["Columns"] = new List<object> { 1 } }, NullLogger.Instance)
            .IsFailure.ShouldBeTrue();
        PipelineRunParameterValues.Verify(declared, new Dictionary<string, object?> { ["Columns"] = new List<object>() }, NullLogger.Instance)
            .IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ASingleValueParameterStillTakesASingleValueAndRefusesAList()
    {
        PipelineParameterConfiguration[] declared = [new() { Name = "Region", ParameterType = "String" }];

        PipelineRunParameterValues.Resolve(declared, Supplied("Region", "EU"), NullLogger.Instance).IsSuccess.ShouldBeTrue();
        PipelineRunParameterValues.Verify(declared, new Dictionary<string, object?> { ["Region"] = new List<object> { "EU" } }, NullLogger.Instance)
            .IsFailure.ShouldBeTrue();
    }
}
