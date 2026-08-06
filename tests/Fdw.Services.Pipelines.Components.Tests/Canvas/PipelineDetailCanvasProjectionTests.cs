using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Components.Canvas;
using Fdw.Services.Pipelines.Components.Canvas.Projection;
using Fdw.Services.Pipelines.Components.Canvas.Validation;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Components.Tests.Canvas;

/// <summary>
/// Tests for <see cref="PipelineDetailCanvasProjection"/> — the projection
/// <c>PipelineBuilderProvider.LoadExisting</c> actually reaches in production, proving a loaded
/// pipeline's canvas is genuinely populated (source/sink/transform nodes, Flow edges) and immediately
/// re-saveable without edits.
/// </summary>
public sealed class PipelineDetailCanvasProjectionTests
{
    private static PipelineDetailResponse BuildDetail(
        IList<PipelineTransformClientRequest>? transforms = null,
        string pipelineType = "BatchCopy") =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Name = "TestPipeline",
            PipelineType = pipelineType,
            SourceConnectionName = "SourceConn",
            DestinationConnectionName = "DestConn",
            SourceDataSet = "SourceData",
            DestinationDataSet = "DestData",
            Transforms = transforms,
        };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToCanvasBuildsSourceSinkAndTransformNodes()
    {
        var detail = BuildDetail(
        [
            new PipelineTransformClientRequest
            {
                Name = "MapStep",
                OperationType = "Map",
                ExecutionOrder = 1,
                FieldMappings = [new PipelineFieldMappingClientRequest { Name = "m1", SourceField = "CustomerId", DestinationField = "CustomerName" }],
            },
            new PipelineTransformClientRequest
            {
                Name = "FilterStep",
                OperationType = "Filter",
                ExecutionOrder = 2,
                FilterExpression = "Age > 18",
            },
        ]);

        var result = PipelineDetailCanvasProjection.ToCanvas(detail, PipelineCanvasTestFixtures.EditMode);

        result.IsSuccess.ShouldBeTrue();
        var model = result.Value!;
        model.Nodes.Count.ShouldBe(4);
        model.PipelineType.ShouldBe("BatchCopy");

        var validation = PipelineGraphValidator.Validate(model);
        validation.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToCanvasWithNoTransformsConnectsSourceDirectlyToSink()
    {
        var detail = BuildDetail(transforms: []);

        var result = PipelineDetailCanvasProjection.ToCanvas(detail, PipelineCanvasTestFixtures.EditMode);

        result.IsSuccess.ShouldBeTrue();
        var model = result.Value!;
        model.Nodes.Count.ShouldBe(2);
        model.Edges.Count.ShouldBe(1);

        var validation = PipelineGraphValidator.Validate(model);
        validation.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToCanvasWithoutPipelineTypeReturnsFailure()
    {
        var detail = BuildDetail(transforms: [], pipelineType: "");

        var result = PipelineDetailCanvasProjection.ToCanvas(detail, PipelineCanvasTestFixtures.EditMode);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToCanvasWithUnrecognizedTransformOperationTypeReturnsFailure()
    {
        var detail = BuildDetail(
        [
            new PipelineTransformClientRequest { Name = "Bogus", OperationType = "Bogus", ExecutionOrder = 1 },
        ]);

        var result = PipelineDetailCanvasProjection.ToCanvas(detail, PipelineCanvasTestFixtures.EditMode);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToCanvasMapFieldMappingsRoundTripThroughSaveProjection()
    {
        // Why: proves a loaded Map transform's field mappings survive as real, editable canvas state —
        // re-projecting the loaded canvas through PipelineCreateRequestProjection (the Save path)
        // must reproduce the same mapping, not an empty list.
        var detail = BuildDetail(
        [
            new PipelineTransformClientRequest
            {
                Name = "MapStep",
                OperationType = "Map",
                ExecutionOrder = 1,
                FieldMappings = [new PipelineFieldMappingClientRequest { Name = "m1", SourceField = "CustomerId", DestinationField = "CustomerName", IsEnabled = true }],
            },
        ]);

        var canvasResult = PipelineDetailCanvasProjection.ToCanvas(detail, PipelineCanvasTestFixtures.EditMode);
        canvasResult.IsSuccess.ShouldBeTrue();

        var requestResult = PipelineCreateRequestProjection.ToCreateRequest(canvasResult.Value!, "TestPipeline", null);

        requestResult.IsSuccess.ShouldBeTrue();
        var mapTransform = requestResult.Value!.Transforms.Single(t => string.Equals(t.OperationType, "Map", StringComparison.Ordinal));
        mapTransform.FieldMappings.Count.ShouldBe(1);
        mapTransform.FieldMappings[0].SourceField.ShouldBe("CustomerId");
        mapTransform.FieldMappings[0].DestinationField.ShouldBe("CustomerName");
    }
}
