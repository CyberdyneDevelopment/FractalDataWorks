using System.Text.Json;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for the pipeline-level <c>rejectSteps</c> member (FDW-1142): it maps to rows of its own phase, reads back exactly as
/// saved, is judged by the engine's validator, and is patched on its own, never as part of the load unit.
/// </summary>
public sealed class PipelineRejectStepsTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private const string RejectStep = """
        { "order": 1, "name": "Keep rejects", "isEnabled": true, "kind": "BulkInsert", "destinationDataSet": "OrderRejects",
          "options": { "writeColumns": ["Id", "RejectReason", "RejectStage"] }, "valueSources": [] }
        """;

    private static readonly string CreateBody = $$"""
        {
          "name": "Orders", "pipelineType": "BatchCopy", "sourceDataSet": "Staging", "destinationDataSet": "Orders", "runRoles": ["Loader"],
          "loadMode": "Append",
          "rejectSteps": [ {{RejectStep}} ]
        }
        """;

    private static CreatePipelineRequest Create(string json) => JsonSerializer.Deserialize<CreatePipelineRequest>(json, Web)!;

    private static UpdatePipelineRequest Update(string json) => JsonSerializer.Deserialize<UpdatePipelineRequest>(json, Web)!;

    private static MappedPipelineLoad Mapped(IPipelineLoadFields fields)
    {
        var mapped = PipelineLoadMapper.Map(fields, NullLogger.Instance);
        mapped.IsSuccess.ShouldBeTrue(mapped.CurrentMessage);
        return mapped.Value!;
    }

    private static BatchCopyPipelineConfiguration Saved(MappedPipelineLoad load) => new()
    {
        Name = "Orders",
        Implementation = "BatchCopy",
        DestinationDataSet = "Orders",
        LoadMode = load.LoadMode,
        LoadSteps = load.LoadSteps,
        RunParameters = load.RunParameters,
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARejectStepMapsToARowOfThePhaseThatWritesRejectedRecords()
    {
        var load = Mapped(Create(CreateBody));

        var row = load.LoadSteps.ShouldNotBeNull().ShouldHaveSingleItem();
        row.Phase.ShouldBe("Reject");
        row.StepOrder.ShouldBe(1);
        row.Kind.ShouldBe("BulkInsert");
        row.DestinationDataSet.ShouldBe("OrderRejects");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RejectStepsReadBackWithTheSameValuesAndNeverAppearAmongTheLoadSteps()
    {
        var request = Create(CreateBody);

        var read = PipelineDetailMapper.Map(Saved(Mapped(request)));

        read.LoadSteps.ShouldBeNull();
        JsonSerializer.Serialize(read.RejectSteps, Web).ShouldBe(JsonSerializer.Serialize(request.RejectSteps, Web));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ALoadWithARejectStepPassesTheEnginesOwnValidator()
    {
        var load = Mapped(Create(CreateBody));

        var result = PipelineLoadValidator.Validate(load.LoadMode, load.LoadSteps, load.RunParameters, "Orders", NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARejectStepWritingThePipelinesOwnDestinationIsRefused()
    {
        var load = Mapped(Create(CreateBody.Replace("OrderRejects", "Orders", StringComparison.Ordinal)));

        var result = PipelineLoadValidator.Validate(load.LoadMode, load.LoadSteps, load.RunParameters, "Orders", NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("reject destination");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void RejectStepsAloneChangeTheLoadDefinitionButNotTheLoadUnit()
    {
        var body = Update($$"""{ "rejectSteps": [ {{RejectStep}} ] }""");

        PipelineLoadPatch.UnitStated(body).ShouldBeFalse();
        PipelineLoadPatch.Changes(body).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void StatingTheLoadUnitKeepsTheStoredRejectSteps()
    {
        var stored = PipelineLoadPatch.Stored(Saved(Mapped(Create(CreateBody))));
        var body = Update("""{ "loadMode": "Replace" }""");

        var effective = PipelineLoadPatch.Apply(body, Mapped(body), stored);

        effective.LoadMode.ShouldBe("Replace");
        effective.LoadSteps.ShouldNotBeNull().ShouldHaveSingleItem().Phase.ShouldBe("Reject");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void StatingRejectStepsKeepsTheStoredLoadUnit()
    {
        var stored = PipelineLoadPatch.Stored(Saved(Mapped(Create(CreateBody))));
        var body = Update($$"""{ "rejectSteps": [ {{RejectStep.Replace("OrderRejects", "OtherRejects", StringComparison.Ordinal)}} ] }""");

        var effective = PipelineLoadPatch.Apply(body, Mapped(body), stored);

        effective.LoadMode.ShouldBe("Append");
        effective.LoadSteps.ShouldNotBeNull().ShouldHaveSingleItem().DestinationDataSet.ShouldBe("OtherRejects");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnEmptyRejectStepsListRemovesTheStoredOnesAndAbsentKeepsThem()
    {
        var stored = PipelineLoadPatch.Stored(Saved(Mapped(Create(CreateBody))));
        var removing = Update("""{ "rejectSteps": [] }""");
        var keeping = Update("""{ "description": "unchanged" }""");

        PipelineLoadPatch.Apply(removing, Mapped(removing), stored).LoadSteps.ShouldBeNull();
        PipelineLoadPatch.Changes(keeping).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheEffectiveBodyCarriesTheStoredRejectStepsWhereTheUpdateStatesNone()
    {
        var saved = Saved(Mapped(Create(CreateBody)));

        var effective = PipelineLoadPatch.Effective(Update("""{ "loadMode": "Replace" }"""), saved);

        effective.RejectSteps.ShouldNotBeNull().ShouldHaveSingleItem().Name.ShouldBe("Keep rejects");
    }
}
