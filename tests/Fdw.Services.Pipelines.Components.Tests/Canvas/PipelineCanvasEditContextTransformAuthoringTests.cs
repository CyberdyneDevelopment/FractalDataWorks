using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Components.Canvas;
using Fdw.Services.Pipelines.Components.Canvas.Projection;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Components.Tests.Canvas;

/// <summary>
/// Tests for the transform authoring additions on <see cref="PipelineCanvasEditContext"/>:
/// PopulateTransformPorts, SetFilterExpression, SetLookup, and the auto-reserialize behavior on
/// Connect/DeleteEdge for FieldMapping edges touching a Map transform node.
/// </summary>
public sealed class PipelineCanvasEditContextTransformAuthoringTests
{
    private static PipelineCanvasModel BuildEditableModel() =>
        new("m1", "Test", PipelineCanvasTestFixtures.EditMode);

    private static async Task<string> AddTransformNode(PipelineCanvasEditContext ctx, string operationType)
    {
        var addResult = await ctx.AddNode(PipelineCanvasTestFixtures.TransformNodeType, "Step", 0, 0, TestContext.Current.CancellationToken);
        var nodeId = addResult.Value!;
        await ctx.UpdateNodeMetadata(
            nodeId,
            new Dictionary<string, string> { [PipelineCanvasMetadataKeys.OperationType] = operationType },
            TestContext.Current.CancellationToken);
        return nodeId;
    }

    // ── PopulateTransformPorts ────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task PopulateTransformPortsSetsInAndOutPorts()
    {
        var model = BuildEditableModel();
        var ctx = (PipelineCanvasEditContext)model.EditContext!;
        var nodeId = await AddTransformNode(ctx, "Map");

        var result = await ctx.PopulateTransformPorts(nodeId, ["CustomerId", "CustomerName"], ["FullName"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var node = model.Nodes.OfType<PipelineCanvasNode>().Single(n => string.Equals(n.Id, nodeId, StringComparison.Ordinal));
        node.Ports.Select(p => p.Id).ShouldContain("in:CustomerId");
        node.Ports.Select(p => p.Id).ShouldContain("in:CustomerName");
        node.Ports.Select(p => p.Id).ShouldContain("out:FullName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task PopulateTransformPortsUnknownNodeReturnsFailure()
    {
        var model = BuildEditableModel();
        var ctx = (PipelineCanvasEditContext)model.EditContext!;

        var result = await ctx.PopulateTransformPorts("ghost", ["Field1"], [], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task PopulateTransformPortsWithNoFieldsReturnsFailure()
    {
        var model = BuildEditableModel();
        var ctx = (PipelineCanvasEditContext)model.EditContext!;
        var nodeId = await AddTransformNode(ctx, "Map");

        var result = await ctx.PopulateTransformPorts(nodeId, [], [], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── SetFilterExpression ───────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SetFilterExpressionWritesConfigPayload()
    {
        var model = BuildEditableModel();
        var ctx = (PipelineCanvasEditContext)model.EditContext!;
        var nodeId = await AddTransformNode(ctx, "Filter");

        var result = await ctx.SetFilterExpression(nodeId, "Age > 18", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var node = model.Nodes.OfType<PipelineCanvasNode>().Single(n => string.Equals(n.Id, nodeId, StringComparison.Ordinal));
        var payload = node.Metadata[PipelineCanvasMetadataKeys.ConfigPayload];
        var parsed = TransformConfigPayloadSerializer.FromConfigPayload("Filter", payload);
        parsed.IsSuccess.ShouldBeTrue();
        parsed.Value!.FilterExpression.ShouldBe("Age > 18");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SetFilterExpressionWrongOperationTypeReturnsFailure()
    {
        var model = BuildEditableModel();
        var ctx = (PipelineCanvasEditContext)model.EditContext!;
        var nodeId = await AddTransformNode(ctx, "Map");

        var result = await ctx.SetFilterExpression(nodeId, "Age > 18", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SetFilterExpressionMissingOperationTypeReturnsFailure()
    {
        var model = BuildEditableModel();
        var ctx = (PipelineCanvasEditContext)model.EditContext!;
        var addResult = await ctx.AddNode(PipelineCanvasTestFixtures.TransformNodeType, "Step", 0, 0, TestContext.Current.CancellationToken);

        var result = await ctx.SetFilterExpression(addResult.Value!, "Age > 18", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SetFilterExpressionUnknownNodeReturnsFailure()
    {
        var model = BuildEditableModel();
        var ctx = (PipelineCanvasEditContext)model.EditContext!;

        var result = await ctx.SetFilterExpression("ghost", "Age > 18", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── SetLookup ─────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SetLookupWritesConfigPayload()
    {
        var model = BuildEditableModel();
        var ctx = (PipelineCanvasEditContext)model.EditContext!;
        var nodeId = await AddTransformNode(ctx, "Lookup");
        var lookup = new LookupClientRequest
        {
            LookupConnectionName = "LookupConn",
            LookupDataSet = "Products",
            LookupKeyField = "ProductId",
            SourceKeyField = "ProductId",
            JoinType = "Inner",
        };

        var result = await ctx.SetLookup(nodeId, lookup, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var node = model.Nodes.OfType<PipelineCanvasNode>().Single(n => string.Equals(n.Id, nodeId, StringComparison.Ordinal));
        var payload = node.Metadata[PipelineCanvasMetadataKeys.ConfigPayload];
        var parsed = TransformConfigPayloadSerializer.FromConfigPayload("Lookup", payload);
        parsed.IsSuccess.ShouldBeTrue();
        parsed.Value!.Lookup!.LookupDataSet.ShouldBe("Products");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SetLookupWrongOperationTypeReturnsFailure()
    {
        var model = BuildEditableModel();
        var ctx = (PipelineCanvasEditContext)model.EditContext!;
        var nodeId = await AddTransformNode(ctx, "Map");
        var lookup = new LookupClientRequest
        {
            LookupConnectionName = "LookupConn",
            LookupDataSet = "Products",
            LookupKeyField = "ProductId",
            SourceKeyField = "ProductId",
            JoinType = "Inner",
        };

        var result = await ctx.SetLookup(nodeId, lookup, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── Auto-reserialize on Connect/DeleteEdge ───────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ConnectFieldMappingEdgeOnMapTransformAutoReserializesConfigPayload()
    {
        var model = BuildEditableModel();
        var ctx = (PipelineCanvasEditContext)model.EditContext!;
        var nodeId = await AddTransformNode(ctx, "Map");
        await ctx.PopulateTransformPorts(nodeId, ["CustomerId"], ["CustomerName"], TestContext.Current.CancellationToken);

        var connectResult = await ctx.Connect(
            nodeId, nodeId, PipelineCanvasTestFixtures.FieldMappingEdgeType,
            "in:CustomerId", "out:CustomerName", TestContext.Current.CancellationToken);

        connectResult.IsSuccess.ShouldBeTrue();
        var node = model.Nodes.OfType<PipelineCanvasNode>().Single(n => string.Equals(n.Id, nodeId, StringComparison.Ordinal));
        node.Metadata.ShouldContainKey(PipelineCanvasMetadataKeys.ConfigPayload);
        var parsed = TransformConfigPayloadSerializer.FromConfigPayload("Map", node.Metadata[PipelineCanvasMetadataKeys.ConfigPayload]);
        parsed.IsSuccess.ShouldBeTrue();
        parsed.Value!.Mappings.Count.ShouldBe(1);
        parsed.Value!.Mappings[0].SourceField.ShouldBe("CustomerId");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task DeleteEdgeOnMapTransformAutoReserializesConfigPayload()
    {
        var model = BuildEditableModel();
        var ctx = (PipelineCanvasEditContext)model.EditContext!;
        var nodeId = await AddTransformNode(ctx, "Map");
        await ctx.PopulateTransformPorts(nodeId, ["CustomerId", "Email"], ["CustomerName", "EmailAddress"], TestContext.Current.CancellationToken);

        var edge1 = await ctx.Connect(nodeId, nodeId, PipelineCanvasTestFixtures.FieldMappingEdgeType, "in:CustomerId", "out:CustomerName", TestContext.Current.CancellationToken);
        var edge2 = await ctx.Connect(nodeId, nodeId, PipelineCanvasTestFixtures.FieldMappingEdgeType, "in:Email", "out:EmailAddress", TestContext.Current.CancellationToken);

        var deleteResult = await ctx.DeleteEdge(edge1.Value!, TestContext.Current.CancellationToken);

        deleteResult.IsSuccess.ShouldBeTrue();
        var node = model.Nodes.OfType<PipelineCanvasNode>().Single(n => string.Equals(n.Id, nodeId, StringComparison.Ordinal));
        var parsed = TransformConfigPayloadSerializer.FromConfigPayload("Map", node.Metadata[PipelineCanvasMetadataKeys.ConfigPayload]);
        parsed.IsSuccess.ShouldBeTrue();
        parsed.Value!.Mappings.Count.ShouldBe(1);
        parsed.Value!.Mappings[0].SourceField.ShouldBe("Email");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ConnectFieldMappingWithUnresolvablePortsRollsBackEdgeAndFails()
    {
        var model = BuildEditableModel();
        var ctx = (PipelineCanvasEditContext)model.EditContext!;
        var nodeId = await AddTransformNode(ctx, "Map");

        var connectResult = await ctx.Connect(
            nodeId, nodeId, PipelineCanvasTestFixtures.FieldMappingEdgeType,
            "CustomerId", "out:CustomerName", TestContext.Current.CancellationToken);

        connectResult.IsSuccess.ShouldBeFalse();
        model.Edges.ShouldBeEmpty();
    }
}
