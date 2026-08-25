using System.Collections.Generic;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Components.Canvas;
using Fdw.Services.Pipelines.Components.Canvas.Projection;
using Fdw.UI.Abstractions.Canvas;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Components.Tests.Canvas;

/// <summary>
/// Tests for <see cref="TransformConfigPayloadSerializer"/>: ToConfigPayload/FromConfigPayload for
/// all five operation types, round-tripping, and the fail-loud paths.
/// </summary>
public sealed class TransformConfigPayloadSerializerTests
{
    private static PipelineCanvasNode BuildTransformPlaceholder(string operationType) =>
        new("t1", CanvasNodeTypes.ByName("Transform"), "Step", operationType, 0, 0, [], new Dictionary<string, string>());

    // ── Map ───────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToConfigPayloadMapSerializesFieldMappingEdges()
    {
        var node = BuildTransformPlaceholder("Map");
        var edge = new PipelineCanvasEdge("e1", "t1", "t1", PipelineCanvasTestFixtures.FieldMappingEdgeType, "in:CustomerId", "out:CustomerName");

        var result = TransformConfigPayloadSerializer.ToConfigPayload("Map", node, [edge]);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("CustomerId");
        result.Value!.ShouldContain("CustomerName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MapConfigPayloadRoundTripsThroughFromConfigPayload()
    {
        var node = BuildTransformPlaceholder("Map");
        var edge = new PipelineCanvasEdge("e1", "t1", "t1", PipelineCanvasTestFixtures.FieldMappingEdgeType, "in:CustomerId", "out:CustomerName");

        var toResult = TransformConfigPayloadSerializer.ToConfigPayload("Map", node, [edge]);
        toResult.IsSuccess.ShouldBeTrue();

        var fromResult = TransformConfigPayloadSerializer.FromConfigPayload("Map", toResult.Value!);

        fromResult.IsSuccess.ShouldBeTrue();
        fromResult.Value!.Mappings.Count.ShouldBe(1);
        fromResult.Value!.Mappings[0].SourceField.ShouldBe("CustomerId");
        fromResult.Value!.Mappings[0].DestinationField.ShouldBe("CustomerName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToConfigPayloadMapWithUnresolvablePortReturnsFailure()
    {
        var node = BuildTransformPlaceholder("Map");
        // Why: SourcePortId lacks the "in:" prefix — unresolvable port field.
        var edge = new PipelineCanvasEdge("e1", "t1", "t1", PipelineCanvasTestFixtures.FieldMappingEdgeType, "CustomerId", "out:CustomerName");

        var result = TransformConfigPayloadSerializer.ToConfigPayload("Map", node, [edge]);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToConfigPayloadMapWithNoMappingsSerializesEmptyArray()
    {
        var node = BuildTransformPlaceholder("Map");

        var result = TransformConfigPayloadSerializer.ToConfigPayload("Map", node, []);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("[]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToConfigPayloadMapWithAbsentMetadataUsesDeclaredDefaults()
    {
        // Why: absent IsRequired/IsEnabled/MappingName is a legitimate "not yet overridden" state
        // (PipelineCanvasEdge starts with empty metadata) — must still resolve to the DTO's own
        // declared defaults (IsRequired=false, IsEnabled=true) and a generated Name.
        var node = BuildTransformPlaceholder("Map");
        var edge = new PipelineCanvasEdge("e1", "t1", "t1", PipelineCanvasTestFixtures.FieldMappingEdgeType, "in:CustomerId", "out:CustomerName");

        var toResult = TransformConfigPayloadSerializer.ToConfigPayload("Map", node, [edge]);
        toResult.IsSuccess.ShouldBeTrue();

        var fromResult = TransformConfigPayloadSerializer.FromConfigPayload("Map", toResult.Value!);
        fromResult.IsSuccess.ShouldBeTrue();
        fromResult.Value!.Mappings[0].IsRequired.ShouldBeFalse();
        fromResult.Value!.Mappings[0].IsEnabled.ShouldBeTrue();
        fromResult.Value!.Mappings[0].Name.ShouldBe("CustomerId->CustomerName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToConfigPayloadMapWithUnparseableIsRequiredReturnsFailure()
    {
        // Why: a PRESENT-but-garbled IsRequired value must fail loud, not silently coerce to false —
        // this is the "disabled mapping persists as enabled" class of corruption bug.
        var node = BuildTransformPlaceholder("Map");
        var metadata = new Dictionary<string, string> { [PipelineCanvasEdgeMetadataKeys.IsRequired] = "not-a-bool" };
        var edge = new PipelineCanvasEdge("e1", "t1", "t1", PipelineCanvasTestFixtures.FieldMappingEdgeType, "in:CustomerId", "out:CustomerName", metadata: metadata);

        var result = TransformConfigPayloadSerializer.ToConfigPayload("Map", node, [edge]);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToConfigPayloadMapWithUnparseableIsEnabledReturnsFailure()
    {
        var node = BuildTransformPlaceholder("Map");
        var metadata = new Dictionary<string, string> { [PipelineCanvasEdgeMetadataKeys.IsEnabled] = "not-a-bool" };
        var edge = new PipelineCanvasEdge("e1", "t1", "t1", PipelineCanvasTestFixtures.FieldMappingEdgeType, "in:CustomerId", "out:CustomerName", metadata: metadata);

        var result = TransformConfigPayloadSerializer.ToConfigPayload("Map", node, [edge]);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToConfigPayloadMapWithBlankMappingNameReturnsFailure()
    {
        // Why: an explicitly-present-but-blank MappingName is an invalid override, distinct from a
        // genuinely absent key (which legitimately derives a generated name).
        var node = BuildTransformPlaceholder("Map");
        var metadata = new Dictionary<string, string> { [PipelineCanvasEdgeMetadataKeys.MappingName] = "   " };
        var edge = new PipelineCanvasEdge("e1", "t1", "t1", PipelineCanvasTestFixtures.FieldMappingEdgeType, "in:CustomerId", "out:CustomerName", metadata: metadata);

        var result = TransformConfigPayloadSerializer.ToConfigPayload("Map", node, [edge]);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    // ── Filter ────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void FilterConfigPayloadRoundTripsThroughFromConfigPayload()
    {
        var node = BuildTransformPlaceholder("Filter");

        var toResult = TransformConfigPayloadSerializer.ToConfigPayload("Filter", node, [], filterExpression: "Age > 18");
        toResult.IsSuccess.ShouldBeTrue();

        var fromResult = TransformConfigPayloadSerializer.FromConfigPayload("Filter", toResult.Value!);

        fromResult.IsSuccess.ShouldBeTrue();
        fromResult.Value!.FilterExpression.ShouldBe("Age > 18");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToConfigPayloadFilterWithEmptyExpressionReturnsFailure()
    {
        var node = BuildTransformPlaceholder("Filter");

        var result = TransformConfigPayloadSerializer.ToConfigPayload("Filter", node, [], filterExpression: "   ");

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void FromConfigPayloadFilterWithEmptyExpressionReturnsFailure()
    {
        var result = TransformConfigPayloadSerializer.FromConfigPayload("Filter", "\"\"");

        result.IsSuccess.ShouldBeFalse();
    }

    // ── Aggregate ─────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AggregateConfigPayloadRoundTripsThroughFromConfigPayload()
    {
        var node = BuildTransformPlaceholder("Aggregate");
        var aggregation = new AggregationClientRequest
        {
            GroupByFields = ["Region"],
            Aggregations = [new AggregationItemClientRequest { SourceField = "Amount", Function = "Sum", OutputField = "TotalAmount" }],
        };

        var toResult = TransformConfigPayloadSerializer.ToConfigPayload("Aggregate", node, [], aggregation: aggregation);
        toResult.IsSuccess.ShouldBeTrue();

        var fromResult = TransformConfigPayloadSerializer.FromConfigPayload("Aggregate", toResult.Value!);

        fromResult.IsSuccess.ShouldBeTrue();
        fromResult.Value!.Aggregation!.GroupByFields[0].ShouldBe("Region");
        fromResult.Value!.Aggregation!.Aggregations[0].OutputField.ShouldBe("TotalAmount");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToConfigPayloadAggregateWithNullAggregationReturnsFailure()
    {
        var node = BuildTransformPlaceholder("Aggregate");

        var result = TransformConfigPayloadSerializer.ToConfigPayload("Aggregate", node, [], aggregation: null);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToConfigPayloadAggregateWithIncompleteItemReturnsFailure()
    {
        var node = BuildTransformPlaceholder("Aggregate");
        var aggregation = new AggregationClientRequest
        {
            Aggregations = [new AggregationItemClientRequest { SourceField = "Amount", Function = "Sum", OutputField = "" }],
        };

        var result = TransformConfigPayloadSerializer.ToConfigPayload("Aggregate", node, [], aggregation: aggregation);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── Calculate ─────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CalculateConfigPayloadRoundTripsThroughFromConfigPayload()
    {
        var node = BuildTransformPlaceholder("Calculate");
        var calculation = new CalculationClientRequest
        {
            ComputedColumns = [new ComputedColumnClientRequest { OutputField = "FullName", Formula = "FirstName + LastName" }],
        };

        var toResult = TransformConfigPayloadSerializer.ToConfigPayload("Calculate", node, [], calculation: calculation);
        toResult.IsSuccess.ShouldBeTrue();

        var fromResult = TransformConfigPayloadSerializer.FromConfigPayload("Calculate", toResult.Value!);

        fromResult.IsSuccess.ShouldBeTrue();
        fromResult.Value!.Calculation!.ComputedColumns[0].OutputField.ShouldBe("FullName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToConfigPayloadCalculateWithNullCalculationReturnsFailure()
    {
        var node = BuildTransformPlaceholder("Calculate");

        var result = TransformConfigPayloadSerializer.ToConfigPayload("Calculate", node, [], calculation: null);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToConfigPayloadCalculateWithIncompleteColumnReturnsFailure()
    {
        var node = BuildTransformPlaceholder("Calculate");
        var calculation = new CalculationClientRequest
        {
            ComputedColumns = [new ComputedColumnClientRequest { OutputField = "FullName", Formula = "" }],
        };

        var result = TransformConfigPayloadSerializer.ToConfigPayload("Calculate", node, [], calculation: calculation);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── Lookup ────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void LookupConfigPayloadRoundTripsThroughFromConfigPayload()
    {
        var node = BuildTransformPlaceholder("Lookup");
        var lookup = new LookupClientRequest
        {
            LookupConnectionName = "LookupConn",
            LookupDataSet = "Products",
            LookupKeyField = "ProductId",
            SourceKeyField = "ProductId",
            LookupColumns = ["ProductName"],
            JoinType = "Inner",
        };

        var toResult = TransformConfigPayloadSerializer.ToConfigPayload("Lookup", node, [], lookup: lookup);
        toResult.IsSuccess.ShouldBeTrue();

        var fromResult = TransformConfigPayloadSerializer.FromConfigPayload("Lookup", toResult.Value!);

        fromResult.IsSuccess.ShouldBeTrue();
        fromResult.Value!.Lookup!.LookupDataSet.ShouldBe("Products");
        fromResult.Value!.Lookup!.JoinType.ShouldBe("Inner");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToConfigPayloadLookupWithNullLookupReturnsFailure()
    {
        var node = BuildTransformPlaceholder("Lookup");

        var result = TransformConfigPayloadSerializer.ToConfigPayload("Lookup", node, [], lookup: null);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToConfigPayloadLookupWithMissingRequiredFieldReturnsFailure()
    {
        var node = BuildTransformPlaceholder("Lookup");
        var lookup = new LookupClientRequest
        {
            LookupConnectionName = "LookupConn",
            LookupDataSet = "Products",
            LookupKeyField = "ProductId",
            SourceKeyField = "ProductId",
            JoinType = "", // missing required field
        };

        var result = TransformConfigPayloadSerializer.ToConfigPayload("Lookup", node, [], lookup: lookup);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── Unknown operation type ────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToConfigPayloadUnknownOperationTypeReturnsFailure()
    {
        var node = BuildTransformPlaceholder("Bogus");

        var result = TransformConfigPayloadSerializer.ToConfigPayload("Bogus", node, []);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void FromConfigPayloadUnknownOperationTypeReturnsFailure()
    {
        var result = TransformConfigPayloadSerializer.FromConfigPayload("Bogus", "{}");

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void FromConfigPayloadUnparseableJsonReturnsFailure()
    {
        var result = TransformConfigPayloadSerializer.FromConfigPayload("Map", "not-valid-json{{{");

        result.IsSuccess.ShouldBeFalse();
    }
}
