using System.Text.Json;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Pipelines.Endpoints;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for <see cref="PipelineLoadPatch"/> (FDW-1105): which parts of a saved pipeline's load definition an update body
/// replaces and which it leaves alone, the one rule saving a pipeline and checking an edit of it share.
/// </summary>
public sealed class PipelineLoadPatchTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static UpdatePipelineRequest Body(string json) => JsonSerializer.Deserialize<UpdatePipelineRequest>(json, Web)!;

    private static BatchCopyPipelineConfiguration Saved(string? loadMode) =>
        new() { Name = "Nightly", Implementation = "BatchCopy", SourceDataSet = "Orders", DestinationDataSet = "OrdersCopy", LoadMode = loadMode };

    [Theory]
    [InlineData("""{ "loadMode": "Append" }""")]
    [InlineData("""{ "loadOptions": { "keyColumns": ["Id"] } }""")]
    [InlineData("""{ "loadSteps": [] }""")]
    [InlineData("""{ "valueSources": [] }""")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AnyOneOfTheFourPropertiesStatesTheLoadUnit(string json)
    {
        PipelineLoadPatch.UnitStated(Body(json)).ShouldBeTrue();
        PipelineLoadPatch.Changes(Body(json)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void RunParametersAloneChangeTheLoadDefinitionButNotTheLoadUnit()
    {
        var body = Body("""{ "parameters": [ { "name": "Since", "type": "DateTimeOffset", "required": false } ] }""");

        PipelineLoadPatch.UnitStated(body).ShouldBeFalse();
        PipelineLoadPatch.Changes(body).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ABodyThatStatesNeitherLeavesTheLoadDefinitionAlone()
    {
        var body = Body("""{ "description": "nightly copy" }""");

        PipelineLoadPatch.Changes(body).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void WhereTheBodyStatesNoLoadUnitTheStoredOneIsKept()
    {
        var effective = PipelineLoadPatch.Effective(Body("""{ "description": "nightly copy" }"""), Saved("Replace"));

        effective.LoadMode.ShouldBe("Replace");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ABodyThatStatesAnyOfTheUnitReplacesItWholeSoWhatItLeavesOutIsCleared()
    {
        var effective = PipelineLoadPatch.Effective(
            Body("""{ "loadOptions": { "keyColumns": ["Id"] } }"""), Saved("Upsert"));

        effective.LoadMode.ShouldBeNull();
        effective.LoadOptions.ShouldNotBeNull().KeyColumns.ShouldBe(["Id"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void StatedRunParametersReplaceTheStoredOnesAndLeaveTheLoadUnitAlone()
    {
        var effective = PipelineLoadPatch.Effective(
            Body("""{ "parameters": [ { "name": "Since", "type": "DateTimeOffset", "required": false } ] }"""), Saved("Append"));

        effective.LoadMode.ShouldBe("Append");
        effective.Parameters.ShouldNotBeNull().Select(p => p.Name).ShouldBe(["Since"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ADestinationDataSetTheBodyNamesIsTheOneTheSavedPipelineWillHave()
    {
        var existing = PipelineDetailMapper.Map(Saved("Append"));

        PipelineLoadPatch.EffectiveDestinationDataSet(Body("""{ "destinationDataSet": "Archive" }"""), existing).ShouldBe("Archive");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NamingADestinationConnectionReplacesTheWholeBindingSoNoDestinationDataSetRemains()
    {
        var existing = PipelineDetailMapper.Map(Saved("Append"));

        PipelineLoadPatch.EffectiveDestinationDataSet(Body("""{ "destinationConnectionName": "Warehouse" }"""), existing).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ABodyThatNamesNoDestinationLeavesTheSavedOneInPlace()
    {
        var existing = PipelineDetailMapper.Map(Saved("Append"));

        PipelineLoadPatch.EffectiveDestinationDataSet(Body("""{ "description": "x" }"""), existing).ShouldBe("OrdersCopy");
    }
}
