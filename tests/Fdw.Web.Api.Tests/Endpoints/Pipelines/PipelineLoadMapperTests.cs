using System.Text.Json;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for the pipeline-loads API contract (FDW-1110, contract v1 to v4): the request shapes the console sends map to
/// the engine's configuration rows, are judged by the engine's own validator, and read back exactly as saved.
/// </summary>
public sealed class PipelineLoadMapperTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private const string UpsertBody = """
        {
          "name": "Orders", "pipelineType": "BatchCopy", "sourceDataSet": "Staging", "destinationDataSet": "Orders", "runRoles": ["Loader"],
          "loadMode": "Upsert",
          "loadOptions": {
            "keyColumns": ["Id"], "compareColumns": ["Name"], "writeColumns": ["Id", "Name", "Source"],
            "versioning": { "effectiveFromColumn": "ValidFrom", "effectiveToColumn": "ValidTo", "currentFlagColumn": "IsCurrent",
                            "insertOnlyRule": { "column": "Source", "value": "Manual" } },
            "softDelete": { "deletedFlagColumn": "IsDeleted", "deletedAtColumn": "DeletedAt", "mode": "AbsentFromSource", "marker": null }
          },
          "valueSources": [
            { "column": "LoadedAt", "source": { "kind": "CurrentTimestamp" } },
            { "column": "Batch", "source": { "kind": "Constant", "valueType": "String", "value": "nightly" } }
          ],
          "parameters": [ { "name": "Since", "type": "DateTimeOffset", "required": false, "description": "Lower bound of the extract" } ]
        }
        """;

    private const string StepsBody = """
        {
          "name": "Orders", "pipelineType": "BatchCopy", "sourceDataSet": "Staging", "destinationDataSet": "Orders", "runRoles": ["Loader"],
          "loadMode": null, "loadOptions": null, "valueSources": [],
          "loadSteps": [
            { "order": 1, "name": "Upsert orders", "isEnabled": true, "kind": "BulkUpsert", "destinationDataSet": "Orders",
              "options": { "keyColumns": ["Id"], "compareColumns": ["Name"], "writeColumns": ["Id", "Name"] }, "valueSources": [] },
            { "order": 2, "name": "Copy to history", "isEnabled": true, "kind": "InsertFromQuery", "destinationDataSet": "OrderHistory",
              "options": { "sourceDataSet": "Orders", "writeColumns": ["Name"], "filterExpression": "UpdatedAt > {{Since}}" },
              "valueSources": [
                { "column": "Seq", "source": { "kind": "NextSequenceValue", "sequenceName": "HistorySeq", "orderBy": ["Id"], "schema": "dbo", "database": "Archive" } },
                { "column": "Alias", "source": { "kind": "Column", "sourceField": "Name" } } ] },
            { "order": 3, "name": "Summarise", "isEnabled": true, "kind": "Execute", "destinationDataSet": "Orders",
              "options": { "destinationContainer": { "dataStoreName": "Main", "pathName": "dbo", "containerName": "LoadSummary" },
                           "parameters": [ { "name": "@Since", "source": { "kind": "Constant", "valueType": "String", "value": "all" } } ] },
              "valueSources": [] },
            { "order": 4, "name": "Clear staging", "isEnabled": false, "kind": "Truncate", "destinationDataSet": "Staging", "options": { }, "valueSources": [] }
          ],
          "parameters": [ { "name": "Since", "type": "DateTimeOffset", "required": true, "description": null } ]
        }
        """;

    private static CreatePipelineRequest Body(string json) => JsonSerializer.Deserialize<CreatePipelineRequest>(json, Web)!;

    private static MappedPipelineLoad Mapped(CreatePipelineRequest request)
    {
        var mapped = PipelineLoadMapper.Map(request, NullLogger.Instance);
        mapped.IsSuccess.ShouldBeTrue(mapped.CurrentMessage);
        return mapped.Value!;
    }

    // What a pipeline carries once saved: the engine record with the mapped load on it, read back through the detail mapper.
    private static PipelineDetailResponse SavedAndRead(MappedPipelineLoad load) => PipelineDetailMapper.Map(new BatchCopyPipelineConfiguration
    {
        Name = "Orders",
        Implementation = "BatchCopy",
        LoadMode = load.LoadMode,
        LoadSteps = load.LoadSteps,
        RunParameters = load.RunParameters,
    });

    private static string LoadJson(IPipelineLoadFields fields) => JsonSerializer.Serialize(
        new { fields.LoadMode, fields.LoadOptions, fields.LoadSteps, fields.ValueSources, fields.Parameters }, Web);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnUpsertWithEveryOptionReadsBackWithTheSameValues()
    {
        var request = Body(UpsertBody);

        var read = SavedAndRead(Mapped(request));

        LoadJson(read).ShouldBe(LoadJson(request));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void LoadStepsOfEveryKindReadBackWithTheSameValuesInOrder()
    {
        var request = Body(StepsBody);
        request.ValueSources = null;

        var read = SavedAndRead(Mapped(request));

        read.LoadMode.ShouldBeNull();
        read.LoadOptions.ShouldBeNull();
        read.ValueSources.ShouldBeNull();
        LoadJson(read).ShouldBe(LoadJson(request));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnUpsertWithEveryOptionPassesTheEnginesOwnValidator()
    {
        var load = Mapped(Body(UpsertBody));

        var result = PipelineLoadValidator.Validate(load.LoadMode, load.LoadSteps, load.RunParameters, null, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void LoadStepsOfEveryKindPassTheEnginesOwnValidator()
    {
        var load = Mapped(Body(StepsBody));

        var result = PipelineLoadValidator.Validate(load.LoadMode, load.LoadSteps, load.RunParameters, null, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheExecuteStepStoresItsContainerAndItsParametersAsValueSourcesNamedByParameter()
    {
        var steps = Mapped(Body(StepsBody)).LoadSteps!;

        var execute = steps.Single(s => s.Kind == "Execute");

        execute.DestinationDataStoreName.ShouldBe("Main");
        execute.DestinationPathName.ShouldBe("dbo");
        execute.DestinationContainerName.ShouldBe("LoadSummary");
        execute.ValueSources.Select(v => v.ColumnName).ShouldBe(["@Since"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ALoadStepWhoseOrderIsNotItsPositionIsRefusedWithTheStepNamed()
    {
        var request = Body(StepsBody);
        request.LoadSteps![1].Order = 5;

        var mapped = PipelineLoadMapper.Map(request, NullLogger.Instance);

        mapped.IsFailure.ShouldBeTrue();
        mapped.CurrentMessage.ShouldNotBeNull().ShouldContain("Copy to history");
        mapped.CurrentMessage.ShouldNotBeNull().ShouldContain("position 2");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    [InlineData("Append")]
    [InlineData("Replace")]
    public void LoadOptionsAreRefusedForAModeThatTakesNone(string mode)
    {
        var request = Body(UpsertBody);
        request.LoadMode = mode;

        var mapped = PipelineLoadMapper.Map(request, NullLogger.Instance);

        mapped.IsFailure.ShouldBeTrue();
        mapped.CurrentMessage.ShouldNotBeNull().ShouldContain(mode);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AppendWithNothingElseMapsToAModeAndNoLoadRows()
    {
        var load = Mapped(new CreatePipelineRequest { Name = "Orders", LoadMode = "Append" });

        load.LoadMode.ShouldBe("Append");
        load.LoadSteps.ShouldBeNull();
        load.RunParameters.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void LoadStepsTogetherWithALoadModeAreRefusedByTheValidatorNotSilentlyResolved()
    {
        var request = Body(StepsBody);
        request.LoadMode = "Append";
        var load = Mapped(request);

        var result = PipelineLoadValidator.Validate(load.LoadMode, load.LoadSteps, load.RunParameters, null, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Append");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ALoadStepWithNoOptionsMapsToAStepThatStatesNone()
    {
        var request = Body(StepsBody);
        request.LoadSteps![3].Options = null;

        var truncate = Mapped(request).LoadSteps!.Single(s => s.Kind == "Truncate");

        truncate.Columns.ShouldBeEmpty();
    }
}
