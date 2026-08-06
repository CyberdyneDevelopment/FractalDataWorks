using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Pipelines.Components.Canvas;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Components.Tests.Canvas;

/// <summary>
/// Tests for <see cref="PipelineCanvasEditContext"/>: AddNode, Connect, MoveNode, DeleteNode, DeleteEdge.
/// </summary>
public sealed class PipelineCanvasEditContextTests
{
    private static PipelineCanvasModel BuildEditableModel() =>
        // Why: EditMode causes the model constructor to create a PipelineCanvasEditContext automatically.
        new("m1", "Test", PipelineCanvasTestFixtures.EditMode);

    // ── AddNode ───────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AddNodeSucceedsAndReturnsId()
    {
        // Arrange
        var model = BuildEditableModel();
        var ctx = model.EditContext!;

        // Act
        var result = await ctx.AddNode(
            PipelineCanvasTestFixtures.DataSetNodeType,
            "NewDataSet", 100, 200,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AddNodeAppearsInModelNodes()
    {
        // Arrange
        var model = BuildEditableModel();
        var ctx = model.EditContext!;

        // Act
        var result = await ctx.AddNode(
            PipelineCanvasTestFixtures.TransformNodeType,
            "MapStep", 150, 250,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        model.Nodes.ShouldContain(n => string.Equals(n.Id, result.Value, System.StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AddNodeNewNodeHasNoEdgesAttached()
    {
        // Arrange
        var model = BuildEditableModel();
        var ctx = model.EditContext!;

        // Act
        var result = await ctx.AddNode(
            PipelineCanvasTestFixtures.DataSetNodeType,
            "Isolated", 0, 0,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        model.Edges.ShouldNotContain(e =>
            string.Equals(e.SourceNodeId, result.Value, System.StringComparison.Ordinal)
            || string.Equals(e.TargetNodeId, result.Value, System.StringComparison.Ordinal));
    }

    // ── Connect ───────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ConnectAddsFlowEdge()
    {
        // Arrange
        var model = BuildEditableModel();
        var ctx = model.EditContext!;

        var srcResult = await ctx.AddNode(PipelineCanvasTestFixtures.DataSetNodeType, "Src", 0, 0, TestContext.Current.CancellationToken);
        var dstResult = await ctx.AddNode(PipelineCanvasTestFixtures.DataSetNodeType, "Dst", 200, 0, TestContext.Current.CancellationToken);

        // Act
        var connectResult = await ctx.Connect(
            srcResult.Value!,
            dstResult.Value!,
            PipelineCanvasTestFixtures.FlowEdgeType,
            null, null,
            TestContext.Current.CancellationToken);

        // Assert
        connectResult.IsSuccess.ShouldBeTrue();
        connectResult.Value.ShouldNotBeNullOrEmpty();
        model.Edges.ShouldContain(e => string.Equals(e.Id, connectResult.Value, System.StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ConnectUnknownSourceIdReturnsFailure()
    {
        // Arrange
        var model = BuildEditableModel();
        var ctx = model.EditContext!;

        var dstResult = await ctx.AddNode(PipelineCanvasTestFixtures.DataSetNodeType, "Dst", 200, 0, TestContext.Current.CancellationToken);

        // Act
        var connectResult = await ctx.Connect(
            "nonexistent-source",
            dstResult.Value!,
            PipelineCanvasTestFixtures.FlowEdgeType,
            null, null,
            TestContext.Current.CancellationToken);

        // Assert
        connectResult.IsSuccess.ShouldBeFalse();
        connectResult.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ConnectUnknownTargetIdReturnsFailure()
    {
        // Arrange
        var model = BuildEditableModel();
        var ctx = model.EditContext!;

        var srcResult = await ctx.AddNode(PipelineCanvasTestFixtures.DataSetNodeType, "Src", 0, 0, TestContext.Current.CancellationToken);

        // Act
        var connectResult = await ctx.Connect(
            srcResult.Value!,
            "nonexistent-target",
            PipelineCanvasTestFixtures.FlowEdgeType,
            null, null,
            TestContext.Current.CancellationToken);

        // Assert
        connectResult.IsSuccess.ShouldBeFalse();
        connectResult.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    // ── MoveNode ──────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task MoveNodeUpdatesXAndY()
    {
        // Arrange
        var model = BuildEditableModel();
        var ctx = model.EditContext!;

        var addResult = await ctx.AddNode(PipelineCanvasTestFixtures.DataSetNodeType, "Node", 0, 0, TestContext.Current.CancellationToken);
        var nodeId = addResult.Value!;

        // Act
        var moveResult = await ctx.MoveNode(nodeId, 999, 888, TestContext.Current.CancellationToken);

        // Assert
        moveResult.IsSuccess.ShouldBeTrue();
        var node = model.Nodes.OfType<PipelineCanvasNode>()
            .Single(n => string.Equals(n.Id, nodeId, System.StringComparison.Ordinal));
        node.X.ShouldBe(999.0);
        node.Y.ShouldBe(888.0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task MoveNodeUnknownIdReturnsFailure()
    {
        // Arrange
        var model = BuildEditableModel();
        var ctx = model.EditContext!;

        // Act
        var moveResult = await ctx.MoveNode("does-not-exist", 10, 20, TestContext.Current.CancellationToken);

        // Assert
        moveResult.IsSuccess.ShouldBeFalse();
        moveResult.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    // ── DeleteNode ────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task DeleteNodeRemovesNodeFromModel()
    {
        // Arrange
        var model = BuildEditableModel();
        var ctx = model.EditContext!;

        var addResult = await ctx.AddNode(PipelineCanvasTestFixtures.DataSetNodeType, "ToDelete", 0, 0, TestContext.Current.CancellationToken);
        var nodeId = addResult.Value!;

        // Act
        var deleteResult = await ctx.DeleteNode(nodeId, TestContext.Current.CancellationToken);

        // Assert
        deleteResult.IsSuccess.ShouldBeTrue();
        model.Nodes.ShouldNotContain(n => string.Equals(n.Id, nodeId, System.StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task DeleteNodeAlsoRemovesConnectedEdges()
    {
        // Arrange
        var model = BuildEditableModel();
        var ctx = model.EditContext!;

        var srcResult = await ctx.AddNode(PipelineCanvasTestFixtures.DataSetNodeType, "Src", 0, 0, TestContext.Current.CancellationToken);
        var midResult = await ctx.AddNode(PipelineCanvasTestFixtures.TransformNodeType, "Mid", 200, 0, TestContext.Current.CancellationToken);
        var dstResult = await ctx.AddNode(PipelineCanvasTestFixtures.DataSetNodeType, "Dst", 400, 0, TestContext.Current.CancellationToken);

        await ctx.Connect(srcResult.Value!, midResult.Value!, PipelineCanvasTestFixtures.FlowEdgeType, null, null, TestContext.Current.CancellationToken);
        await ctx.Connect(midResult.Value!, dstResult.Value!, PipelineCanvasTestFixtures.FlowEdgeType, null, null, TestContext.Current.CancellationToken);

        model.Edges.Count.ShouldBe(2);

        // Act — delete the middle node
        await ctx.DeleteNode(midResult.Value!, TestContext.Current.CancellationToken);

        // Assert — both connected edges removed
        model.Edges.ShouldBeEmpty();
        model.Nodes.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task DeleteNodeUnknownIdReturnsFailure()
    {
        // Arrange
        var model = BuildEditableModel();
        var ctx = model.EditContext!;

        // Act
        var deleteResult = await ctx.DeleteNode("ghost-node", TestContext.Current.CancellationToken);

        // Assert
        deleteResult.IsSuccess.ShouldBeFalse();
        deleteResult.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    // ── DeleteEdge ────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task DeleteEdgeRemovesEdgeFromModel()
    {
        // Arrange
        var model = BuildEditableModel();
        var ctx = model.EditContext!;

        var srcResult = await ctx.AddNode(PipelineCanvasTestFixtures.DataSetNodeType, "Src", 0, 0, TestContext.Current.CancellationToken);
        var dstResult = await ctx.AddNode(PipelineCanvasTestFixtures.DataSetNodeType, "Dst", 200, 0, TestContext.Current.CancellationToken);
        var connectResult = await ctx.Connect(srcResult.Value!, dstResult.Value!, PipelineCanvasTestFixtures.FlowEdgeType, null, null, TestContext.Current.CancellationToken);

        var edgeId = connectResult.Value!;
        model.Edges.Count.ShouldBe(1);

        // Act
        var deleteResult = await ctx.DeleteEdge(edgeId, TestContext.Current.CancellationToken);

        // Assert
        deleteResult.IsSuccess.ShouldBeTrue();
        model.Edges.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task DeleteEdgeUnknownIdReturnsFailure()
    {
        // Arrange
        var model = BuildEditableModel();
        var ctx = model.EditContext!;

        // Act
        var deleteResult = await ctx.DeleteEdge("ghost-edge", TestContext.Current.CancellationToken);

        // Assert
        deleteResult.IsSuccess.ShouldBeFalse();
        deleteResult.CurrentMessage.ShouldNotBeNullOrEmpty();
    }
}
