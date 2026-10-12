using System.Linq;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for the pipeline types a stage kind runs on (FDW-1120 phase 4): a stage of a kind that does not run on the pipeline's
/// type is refused when it is saved, naming the stage and the path to it, wherever it sits in the tree. (The type-collection
/// value that lists the types is tested in <c>Fdw.Operations.Endpoints.Tests</c>, which can reach the resolver.)
/// </summary>
public sealed class PipelineStageKindPipelineTypesTests
{
    private const char Separator = '\u001F';

    private static CreatePipelineTransformRequest Union(string name = "AddArchive") => new()
    {
        Name = name,
        OperationType = "Union",
        ExecutionOrder = 1,
        UnionSource = "Archive",
        FieldMappings = [new CreatePipelineFieldMappingRequest { Name = "Id", SourceField = "Id", DestinationField = "Id" }]
    };

    private static CreatePipelineTransformRequest Filter(string name = "Keep") =>
        new() { Name = name, OperationType = "Filter", FilterExpression = "Age >= 18", ExecutionOrder = 1 };

    private static CreatePipelineTransformRequest BranchHolding(CreatePipelineTransformRequest stage) => new()
    {
        Name = "Route",
        OperationType = "Branch",
        Unmatched = "Pass",
        ExecutionOrder = 1,
        Paths = [new CreatePipelinePathRequest { Name = "Adults", Condition = "Age >= 18", Stages = [stage] }]
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AUnionOnAStreamingPipelineIsRefusedNamingTheStage()
    {
        // Act
        var result = PipelineTransformConfigurationMapper.Map("Streaming", [Union()], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Details!.GetValue<string>("StepName").ShouldBe("AddArchive");
        var reason = result.Details!.GetValue<string>("Reason").ShouldNotBeNull();
        reason.ShouldContain("Union");
        reason.ShouldContain("Streaming");
        reason.ShouldContain("BatchCopy");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AUnionOnABatchCopyPipelineIsAccepted()
    {
        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [Union()], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Single().UnionSourceDataSet.ShouldBe("Archive");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ThePipelineTypeIsMatchedIgnoringCase()
    {
        // Act
        var result = PipelineTransformConfigurationMapper.Map("batchcopy", [Union()], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AUnionInsideABranchPathOnAStreamingPipelineIsRefusedWithThePathToIt()
    {
        // Act
        var result = PipelineTransformConfigurationMapper.Map("Streaming", [BranchHolding(Union())], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Details!.GetValue<string>("StagePath")!.Split(Separator).ShouldBe(new[] { "Route", "Adults", "AddArchive" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AKindThatStatesNoPipelineTypesRunsOnEveryPipelineType()
    {
        // Act
        var streaming = PipelineTransformConfigurationMapper.Map("Streaming", [Filter()], NullLogger.Instance);
        var batch = PipelineTransformConfigurationMapper.Map("BatchCopy", [Filter()], NullLogger.Instance);

        // Assert
        streaming.IsSuccess.ShouldBeTrue(streaming.CurrentMessage);
        batch.IsSuccess.ShouldBeTrue(batch.CurrentMessage);
    }
}
