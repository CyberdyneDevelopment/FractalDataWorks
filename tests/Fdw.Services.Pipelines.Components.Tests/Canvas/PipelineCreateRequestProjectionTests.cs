using System;
using System.Collections.Generic;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Components.Canvas;
using Fdw.Services.Pipelines.Components.Canvas.Projection;
using Fdw.UI.Abstractions.Canvas;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Components.Tests.Canvas;

/// <summary>
/// Tests for <see cref="PipelineCreateRequestProjection"/>: proves
/// <see cref="TransformConfigPayloadSerializer"/> output round-trips byte-compatibly through the
/// real <c>ApplyConfigPayload</c> reader (exercised via the public <c>ToCreateRequest</c> entry
/// point), and that <see cref="PipelineCanvasModel.PipelineType"/> drives the PipelineType field.
/// </summary>
public sealed class PipelineCreateRequestProjectionTests
{
    private const string SourceConnectionName = "SourceConn";
    private const string SinkConnectionName = "DestConn";

    private static PipelineCanvasNode BuildSourceNode()
    {
        var meta = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [PipelineCanvasMetadataKeys.DataSetRole] = PipelineCanvasMetadataKeys.RoleSource,
            [PipelineCanvasMetadataKeys.DataSetName] = "SourceData",
            [PipelineCanvasMetadataKeys.ConnectionName] = SourceConnectionName,
        };
        return new PipelineCanvasNode("source", CanvasNodeTypes.ByName("DataSet"), "SourceData", "Source", 0, 0, [], meta);
    }

    private static PipelineCanvasNode BuildSinkNode()
    {
        var meta = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [PipelineCanvasMetadataKeys.DataSetRole] = PipelineCanvasMetadataKeys.RoleSink,
            [PipelineCanvasMetadataKeys.DataSetName] = "DestData",
            [PipelineCanvasMetadataKeys.ConnectionName] = SinkConnectionName,
        };
        return new PipelineCanvasNode("sink", CanvasNodeTypes.ByName("DataSet"), "DestData", "Sink", 400, 0, [], meta);
    }

    private static PipelineCanvasNode BuildTransformNode(string operationType, string configPayload)
    {
        var meta = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [PipelineCanvasMetadataKeys.OperationType] = operationType,
            [PipelineCanvasMetadataKeys.ExecutionOrder] = "1",
            [PipelineCanvasMetadataKeys.ConfigPayload] = configPayload,
        };
        return new PipelineCanvasNode("t1", CanvasNodeTypes.ByName("Transform"), "Step", operationType, 200, 0, [], meta);
    }

    private static PipelineCanvasModel BuildModel(PipelineCanvasNode transformNode, string? pipelineType = "BatchCopy") =>
        new("m1", "Test Pipeline", PipelineCanvasTestFixtures.EditMode,
            [BuildSourceNode(), BuildSinkNode(), transformNode], [], pipelineType: pipelineType);

    // ── Map ───────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MapConfigPayloadFromSerializerReadsThroughRealProjection()
    {
        var placeholder = new PipelineCanvasNode("t1", CanvasNodeTypes.ByName("Transform"), "Step", "Map", 0, 0, [], new Dictionary<string, string>());
        var edge = new PipelineCanvasEdge("e1", "t1", "t1", PipelineCanvasTestFixtures.FieldMappingEdgeType, "in:CustomerId", "out:CustomerName");
        var payload = TransformConfigPayloadSerializer.ToConfigPayload("Map", placeholder, [edge]).Value!;

        var model = BuildModel(BuildTransformNode("Map", payload));

        var result = PipelineCreateRequestProjection.ToCreateRequest(model, "TestPipeline", null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Transforms[0].FieldMappings.Count.ShouldBe(1);
        result.Value!.Transforms[0].FieldMappings[0].SourceField.ShouldBe("CustomerId");
        result.Value!.Transforms[0].FieldMappings[0].DestinationField.ShouldBe("CustomerName");
    }

    // ── Filter ────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void FilterConfigPayloadFromSerializerReadsThroughRealProjection()
    {
        var placeholder = new PipelineCanvasNode("t1", CanvasNodeTypes.ByName("Transform"), "Step", "Filter", 0, 0, [], new Dictionary<string, string>());
        var payload = TransformConfigPayloadSerializer.ToConfigPayload("Filter", placeholder, [], filterExpression: "Age > 18").Value!;

        var model = BuildModel(BuildTransformNode("Filter", payload));

        var result = PipelineCreateRequestProjection.ToCreateRequest(model, "TestPipeline", null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Transforms[0].FilterExpression.ShouldBe("Age > 18");
    }

    // ── Aggregate ─────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AggregateConfigPayloadFromSerializerReadsThroughRealProjection()
    {
        var placeholder = new PipelineCanvasNode("t1", CanvasNodeTypes.ByName("Transform"), "Step", "Aggregate", 0, 0, [], new Dictionary<string, string>());
        var aggregation = new AggregationClientRequest
        {
            GroupByFields = ["Region"],
            Aggregations = [new AggregationItemClientRequest { SourceField = "Amount", Function = "Sum", OutputField = "TotalAmount" }],
        };
        var payload = TransformConfigPayloadSerializer.ToConfigPayload("Aggregate", placeholder, [], aggregation: aggregation).Value!;

        var model = BuildModel(BuildTransformNode("Aggregate", payload));

        var result = PipelineCreateRequestProjection.ToCreateRequest(model, "TestPipeline", null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Transforms[0].Aggregation.ShouldNotBeNull();
        result.Value!.Transforms[0].Aggregation!.GroupByFields[0].ShouldBe("Region");
        result.Value!.Transforms[0].Aggregation!.Aggregations[0].OutputField.ShouldBe("TotalAmount");
    }

    // ── Calculate ─────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CalculateConfigPayloadFromSerializerReadsThroughRealProjection()
    {
        var placeholder = new PipelineCanvasNode("t1", CanvasNodeTypes.ByName("Transform"), "Step", "Calculate", 0, 0, [], new Dictionary<string, string>());
        var calculation = new CalculationClientRequest
        {
            ComputedColumns = [new ComputedColumnClientRequest { OutputField = "FullName", Formula = "FirstName + LastName" }],
        };
        var payload = TransformConfigPayloadSerializer.ToConfigPayload("Calculate", placeholder, [], calculation: calculation).Value!;

        var model = BuildModel(BuildTransformNode("Calculate", payload));

        var result = PipelineCreateRequestProjection.ToCreateRequest(model, "TestPipeline", null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Transforms[0].Calculation.ShouldNotBeNull();
        result.Value!.Transforms[0].Calculation!.ComputedColumns[0].OutputField.ShouldBe("FullName");
    }

    // ── Lookup ────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void LookupConfigPayloadFromSerializerReadsThroughRealProjection()
    {
        var placeholder = new PipelineCanvasNode("t1", CanvasNodeTypes.ByName("Transform"), "Step", "Lookup", 0, 0, [], new Dictionary<string, string>());
        var lookup = new LookupClientRequest
        {
            LookupConnectionName = "LookupConn",
            LookupDataSet = "Products",
            LookupKeyField = "ProductId",
            SourceKeyField = "ProductId",
            LookupColumns = ["ProductName"],
            JoinType = "Inner",
        };
        var payload = TransformConfigPayloadSerializer.ToConfigPayload("Lookup", placeholder, [], lookup: lookup).Value!;

        var model = BuildModel(BuildTransformNode("Lookup", payload));

        var result = PipelineCreateRequestProjection.ToCreateRequest(model, "TestPipeline", null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Transforms[0].Lookup.ShouldNotBeNull();
        result.Value!.Transforms[0].Lookup!.LookupDataSet.ShouldBe("Products");
        result.Value!.Transforms[0].Lookup!.JoinType.ShouldBe("Inner");
    }

    // ── PipelineType ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToCreateRequestUsesModelPipelineType()
    {
        var placeholder = new PipelineCanvasNode("t1", CanvasNodeTypes.ByName("Transform"), "Step", "Filter", 0, 0, [], new Dictionary<string, string>());
        var payload = TransformConfigPayloadSerializer.ToConfigPayload("Filter", placeholder, [], filterExpression: "Age > 18").Value!;

        var model = BuildModel(BuildTransformNode("Filter", payload), pipelineType: "Streaming");

        var result = PipelineCreateRequestProjection.ToCreateRequest(model, "TestPipeline", null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.PipelineType.ShouldBe("Streaming");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToCreateRequestWithoutPipelineTypeReturnsFailure()
    {
        var placeholder = new PipelineCanvasNode("t1", CanvasNodeTypes.ByName("Transform"), "Step", "Filter", 0, 0, [], new Dictionary<string, string>());
        var payload = TransformConfigPayloadSerializer.ToConfigPayload("Filter", placeholder, [], filterExpression: "Age > 18").Value!;

        var model = BuildModel(BuildTransformNode("Filter", payload), pipelineType: null);

        var result = PipelineCreateRequestProjection.ToCreateRequest(model, "TestPipeline", null);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    // ── ApplyConfigPayload strengthened validation (matches FromConfigPayload) ────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToCreateRequestWithEmptyFilterPayloadReturnsFailure()
    {
        // Why: FromConfigPayload has always rejected an empty-predicate Filter; ApplyConfigPayload
        // (the reader actually consumed by the persisted create-pipeline request) previously accepted
        // it with no validation at all — an empty-predicate Filter must never be persisted.
        var model = BuildModel(BuildTransformNode("Filter", "\"\""));

        var result = PipelineCreateRequestProjection.ToCreateRequest(model, "TestPipeline", null);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToCreateRequestWithNullAggregationPayloadReturnsFailure()
    {
        var model = BuildModel(BuildTransformNode("Aggregate", "null"));

        var result = PipelineCreateRequestProjection.ToCreateRequest(model, "TestPipeline", null);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToCreateRequestWithNullLookupPayloadReturnsFailure()
    {
        var model = BuildModel(BuildTransformNode("Lookup", "null"));

        var result = PipelineCreateRequestProjection.ToCreateRequest(model, "TestPipeline", null);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToCreateRequestWithNullCalculationPayloadReturnsFailure()
    {
        var model = BuildModel(BuildTransformNode("Calculate", "null"));

        var result = PipelineCreateRequestProjection.ToCreateRequest(model, "TestPipeline", null);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }
}
