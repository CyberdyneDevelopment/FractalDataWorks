using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-3931: a value a data set request's caller chooses (a new source's kind, priority and pushdown, a new field's nullability, a
/// filter's operator and data type) has no default: left out, the request is refused, naming it. A source or field the data set
/// already has keeps its own.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "DataIntegrity")]
public sealed class DataSetRequestValuesTests
{
    private static DataSetRequestValues Values() => new("Orders", NullLogger.Instance);

    private static CreateDataSetSourceRequest Source(string name) =>
        new() { SourceName = name, SourceKind = "DataStore", Priority = 4, SupportsPredicatePushdown = false };

    [Fact]
    public void ANewSourceKeepsWhatItStates()
    {
        var mapped = DataSetQueryHelper.MapSources([Source("Main")], [], Values());

        mapped.IsSuccess.ShouldBeTrue();
        var source = mapped.Value!.Single();
        source.SourceKind.ShouldBe("DataStore");
        source.Priority.ShouldBe(4);
        source.SupportsPredicatePushdown.ShouldBeFalse();
    }

    [Theory]
    [InlineData("sourceKind")]
    [InlineData("priority")]
    [InlineData("supportsPredicatePushdown")]
    public void ANewSourceThatLeavesOutAChoiceIsRefusedNamingIt(string field)
    {
        var source = Source("Main");
        switch (field)
        {
            case "sourceKind": source.SourceKind = null; break;
            case "priority": source.Priority = null; break;
            default: source.SupportsPredicatePushdown = null; break;
        }

        var mapped = DataSetQueryHelper.MapSources([source], [], Values());

        mapped.IsSuccess.ShouldBeFalse();
        mapped.Code!.Name.ShouldBe("ValueRequired");
        mapped.Details!.GetValue<string>("field").ShouldBe(field);
    }

    [Fact]
    public void ASourceTheDataSetHasKeepsWhatTheRequestDoesNotState()
    {
        var stored = new DataSetSourceConfiguration { SourceName = "Main", SourceKind = "DataSet", Priority = 3, SupportsPredicatePushdown = true };

        var mapped = DataSetQueryHelper.MapSources([new CreateDataSetSourceRequest { SourceName = "Main" }], [stored], Values());

        mapped.IsSuccess.ShouldBeTrue();
        var source = mapped.Value!.Single();
        source.SourceKind.ShouldBe("DataSet");
        source.Priority.ShouldBe(3);
        source.SupportsPredicatePushdown.ShouldBeTrue();
    }

    [Fact]
    public void ANewFieldThatDoesNotStateItsNullabilityIsRefused()
    {
        var mapped = DataSetQueryHelper.MapFields(
            [new CreateDataSetFieldRequest { Name = "A", DataType = "Int32", IsFilterable = true }], [], Values());

        mapped.IsSuccess.ShouldBeFalse();
        mapped.Code!.Name.ShouldBe("ValueRequired");
        mapped.Details!.GetValue<string>("field").ShouldBe("isNullable");
    }

    [Fact]
    public void AFieldTheDataSetHasKeepsItsNullability()
    {
        var mapped = DataSetQueryHelper.MapFields(
            [new CreateDataSetFieldRequest { Name = "A", DataType = "Int32", IsFilterable = true }],
            [new DataSetFieldConfiguration { Name = "A", IsNullable = true }],
            Values());

        mapped.IsSuccess.ShouldBeTrue();
        mapped.Value!.Single().IsNullable.ShouldBeTrue();
    }

    [Fact]
    public void AStatedNullabilityIsUsed()
    {
        var mapped = DataSetQueryHelper.MapFields(
            [new CreateDataSetFieldRequest { Name = "A", DataType = "Int32", IsFilterable = true, IsNullable = false }], [], Values());

        mapped.Value!.Single().IsNullable.ShouldBeFalse();
    }

    [Theory]
    [InlineData("", "String", "operator")]
    [InlineData("Equals", "", "dataType")]
    public void AFilterThatLeavesOutItsOperatorOrDataTypeIsRefused(string op, string dataType, string field)
    {
        var result = DataSetQueryHelper.ValidateFilters(
            [new DataSetFilterConditionPayload { FieldName = "Region", Operator = op, DataType = dataType }], "Orders", NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
        result.Details!.GetValue<string>("field").ShouldBe(field);
    }

    [Fact]
    public void ACloneKeepsTheFieldsNullability()
        => new DataSetFieldConfiguration { Name = "A", IsNullable = false }.Clone().IsNullable.ShouldBeFalse();

    [Fact]
    public void AnUnstatedVersionIsTheKindsDeclaredVersion()
    {
        // FDW-3956: the data set kind declares the version a request leaves out.
        var version = Values().DeclaredText(null, "Simple", DataSetDeclaredDefaults.Version, "version");

        version.IsSuccess.ShouldBeTrue();
        version.Value.ShouldBe("1.0");
    }

    [Fact]
    public void AStatedVersionIsUsedAsItIs()
        => Values().DeclaredText("2.1", "Simple", DataSetDeclaredDefaults.Version, "version").Value.ShouldBe("2.1");

    [Fact]
    public void ACachingBlockTakesTheKindsDeclaredValuesForWhatItLeavesOut()
    {
        var caching = DataSetQueryHelper.MapCaching(new DataSetCachingPayload { DurationMinutes = 5 }, "Simple", Values());

        caching.IsSuccess.ShouldBeTrue();
        caching.Value!.Enabled.ShouldBeTrue();
        caching.Value.DurationMinutes.ShouldBe(5);
        caching.Value.KeyPattern.ShouldBe("dataset:{datasetName}:{queryHash}");
    }

    [Fact]
    public void NoCachingBlockIsNoCaching()
        => DataSetQueryHelper.MapCaching(null, "Simple", Values()).Value.ShouldBeNull();
}
