using System.Collections.Generic;
using System.Linq;
using Fdw.UI.Abstractions.Canvas;
using Fdw.UI.Abstractions.RenderModeOptions;
using Fdw.UI.Canvas.Blazor.Renderers.NodeGraph;
using Fdw.UI.Canvas.Blazor.Tests.Fakes;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.UI.Canvas.Blazor.Tests;

/// <summary>
/// Unit tests for <c>BlazorDiagramsRenderer</c>.
/// </summary>
/// <remarks>
/// These are not bUnit render tests. The vendor <c>DiagramCanvas</c> measures its container via the
/// browser DOM (ResizeObserver + getBoundingClientRect) in <c>OnAfterRenderAsync</c>; under bUnit there
/// is no DOM, so the vendor component throws inside its own post-render layout. Driving it to completion
/// would test Z.Blazor.Diagrams, not this renderer. The renderer's own contract is the diagram-build
/// mapping (canvas nodes/edges → vendor models, orphan-edge skipping, lock-in-view-mode), asserted
/// directly here via the internal <c>BuildDiagram</c>. Live <c>DiagramCanvas</c> rendering is covered by
/// the Playwright E2E suite. The registry/registration wiring is asserted by the last test (no render).
/// </remarks>
public sealed class BlazorDiagramsRendererTests
{
    private static BlazorDiagramsRenderer NewRenderer() => new();

    private static IRenderMode EditMode()
    {
        var mock = new Mock<IRenderMode>();
        mock.Setup(m => m.Name).Returns("Edit");
        mock.Setup(m => m.AllowsEditing).Returns(true);
        mock.Setup(m => m.ShowsView).Returns(true);
        return mock.Object;
    }

    // ── 1. Nodes and edges are mapped to vendor models ─────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void BuildDiagramMapsNodesAndEdges()
    {
        // Arrange — FakeCanvasModel carries 2 nodes ("Source", "Target") + 1 edge between them.
        var model = new FakeCanvasModel();

        // Act
        var diagram = NewRenderer().BuildDiagram(model);

        // Assert: both nodes mapped with their labels as titles, and the edge mapped to a link.
        diagram.Nodes.Count().ShouldBe(2);
        diagram.Nodes.Select(n => n.Title).ShouldBe(["Source", "Target"], ignoreOrder: true);
        diagram.Links.Count().ShouldBe(1);
    }

    // ── 2. Orphaned edges (missing endpoint) are skipped, not mapped ────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void BuildDiagramSkipsOrphanedEdges()
    {
        // Arrange — an edge whose target node does not exist in the model.
        var model = new FakeCanvasModel
        {
            Edges =
            [
                new FakeCanvasEdge
                {
                    Id = "edge-orphan",
                    SourceNodeId = "node-1",
                    TargetNodeId = "does-not-exist",
                    EdgeType = CanvasEdgeTypes.ByName("Flow"),
                },
            ],
        };

        // Act
        var diagram = NewRenderer().BuildDiagram(model);

        // Assert: the orphaned edge is skipped — nodes still map, but no link is created.
        diagram.Nodes.Count().ShouldBe(2);
        diagram.Links.Count().ShouldBe(0);
    }

    // ── 3. Nodes are locked in view mode, unlocked in edit mode ────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void BuildDiagramLocksNodesInViewModeAndUnlocksInEditMode()
    {
        // Arrange: FakeCanvasModel defaults to view mode (AllowsEditing == false).
        var viewModel = new FakeCanvasModel();
        var editModel = new FakeCanvasModel { RenderMode = EditMode() };

        // Act
        var viewDiagram = NewRenderer().BuildDiagram(viewModel);
        var editDiagram = NewRenderer().BuildDiagram(editModel);

        // Assert: view mode locks every node (no drag); edit mode leaves them draggable.
        viewDiagram.Nodes.ShouldAllBe(n => n.Locked);
        editDiagram.Nodes.ShouldAllBe(n => !n.Locked);
    }

    // ── 4. Empty model builds an empty diagram without throwing ─────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void EmptyModelBuildsEmptyDiagram()
    {
        // Arrange
        var model = new FakeCanvasModel
        {
            Nodes = [],
            Edges = [],
        };

        // Act
        var diagram = NewRenderer().BuildDiagram(model);

        // Assert
        diagram.Nodes.Count().ShouldBe(0);
        diagram.Links.Count().ShouldBe(0);
    }

    // ── 5. Diagrams TypeOption is registered in CanvasRendererTypes ────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Ui")]
    public void DiagramsRendererTypeIsRegisteredInCanvasRendererTypes()
    {
        // Arrange / Act: check the TypeCollection registry — no render needed.
        var descriptor = CanvasRendererTypes.ByName("Diagrams");

        // Assert
        descriptor.ShouldNotBe(
            CanvasRendererTypes.NotFound,
            "Expected the 'Diagrams' renderer TypeOption to be registered in CanvasRendererTypes");
        descriptor.RenderComponentType.ShouldBe(
            typeof(BlazorDiagramsRenderer),
            "Expected BlazorDiagramsRendererType.RenderComponentType to point to BlazorDiagramsRenderer");
    }
}
