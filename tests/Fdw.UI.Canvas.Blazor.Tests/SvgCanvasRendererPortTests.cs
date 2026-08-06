using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Bunit;
using TestContext = Bunit.BunitContext;
using Fdw.Results;
using Fdw.UI.Abstractions.Canvas;
using Fdw.UI.Abstractions.RenderModeOptions;
using Fdw.UI.Canvas.Blazor.RendererTypes;
using Fdw.UI.Canvas.Blazor.Renderers.Svg;
using Fdw.UI.Canvas.Blazor.Tests.Fakes;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.UI.Canvas.Blazor.Tests;

/// <summary>
/// bUnit tests for <c>SvgCanvasRenderer</c>'s port rendering, port-anchored edge geometry, and
/// the port-connect (field mapping) gesture.
/// </summary>
public sealed class SvgCanvasRendererPortTests
{
    // ── Shared helpers ─────────────────────────────────────────────────────────────

    private const string FieldMappingStroke = "#a855f7";

    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    private static IRenderMode CreateEditMode()
    {
        // Why: mirrors FakeCanvasModel's own view-mode mock — IRenderMode is a TypeOption and a
        // stand-alone mock avoids depending on TypeCollection registration order in tests.
        var mock = new Mock<IRenderMode>();
        mock.Setup(m => m.Name).Returns("Edit");
        mock.Setup(m => m.AllowsEditing).Returns(true);
        mock.Setup(m => m.ShowsView).Returns(true);
        return mock.Object;
    }

    private static FakeCanvasPort InPort(string field) =>
        new() { Id = $"in:{field}", Name = field, Direction = PortDirections.ByName("In") };

    private static FakeCanvasPort OutPort(string field) =>
        new() { Id = $"out:{field}", Name = field, Direction = PortDirections.ByName("Out") };

    private static FakeCanvasNode TransformNode(params ICanvasPort[] ports) =>
        new()
        {
            Id = "t1",
            Label = "MapTransform",
            NodeType = CanvasNodeTypes.ByName("Transform"),
            X = 200,
            Y = 200,
            Ports = ports,
        };

    private static Mock<ICanvasEditContext> CreateEditContext()
    {
        // Why: the success result is mocked rather than built with GenericResult.Success — the
        // concrete result type lives in Fdw.Results, which this test project does not reference,
        // and the renderer only ever reads IsSuccess/CurrentMessage off the returned contract.
        var successResult = new Mock<IGenericResult<string>>();
        successResult.Setup(r => r.IsSuccess).Returns(true);
        successResult.Setup(r => r.Value).Returns("new-edge");

        var mock = new Mock<ICanvasEditContext>();
        mock.Setup(c => c.Connect(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ICanvasEdgeType>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult.Object);
        return mock;
    }

    // ── 1. The descriptor advertises port support ──────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void SvgRendererTypeAdvertisesPortSupport()
    {
        new SvgCanvasRendererType().SupportsPorts.ShouldBeTrue(
            "The SVG renderer draws ports and authors port-to-port FieldMapping edges");
    }

    // ── 2. Ports render as discs ───────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void PortsRenderAsCirclesForNodeWithPorts()
    {
        // Arrange
        using var ctx = CreateContext();
        var model = new FakeCanvasModel
        {
            Nodes = [TransformNode(InPort("CustomerId"), OutPort("CustId"))],
            Edges = [],
        };

        // Act
        var cut = ctx.Render<SvgCanvasRenderer>(p => p.Add(r => r.Model, model));

        // Assert: exactly one disc per port. The node is a Hexagon and nothing is selected, so no
        // other <circle> is in play.
        cut.FindAll("circle").Count.ShouldBe(2,
            "Expected one <circle> per port on the Transform node");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void PortLabelsRenderForEachPort()
    {
        // Arrange
        using var ctx = CreateContext();
        var model = new FakeCanvasModel
        {
            Nodes = [TransformNode(InPort("CustomerId"), OutPort("CustId"))],
            Edges = [],
        };

        // Act
        var cut = ctx.Render<SvgCanvasRenderer>(p => p.Add(r => r.Model, model));

        // Assert
        cut.Markup.ShouldContain("CustomerId");
        cut.Markup.ShouldContain("CustId");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Ui")]
    public void NodeWithNoPortsRendersNoPortCircles()
    {
        // Arrange: the default fake nodes carry no ports.
        using var ctx = CreateContext();
        var model = new FakeCanvasModel { Edges = [] };

        // Act
        var cut = ctx.Render<SvgCanvasRenderer>(p => p.Add(r => r.Model, model));

        // Assert
        cut.FindAll("circle").ShouldBeEmpty(
            "A node without ports must not render any port discs");
    }

    // ── 3. Body height ─────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void NodeBodyGrowsToContainTallPortColumn()
    {
        // Arrange: 6 in-ports → half-height = max(20, (5/2)*16 + 10) = 50.
        using var ctx = CreateContext();
        var model = new FakeCanvasModel
        {
            Nodes =
            [
                TransformNode(
                    InPort("F1"), InPort("F2"), InPort("F3"),
                    InPort("F4"), InPort("F5"), InPort("F6")),
            ],
            Edges = [],
        };

        // Act
        var cut = ctx.Render<SvgCanvasRenderer>(p => p.Add(r => r.Model, model));

        // Assert: the Hexagon's apex tracks the grown half-height (halfHeight + 2 = 52).
        cut.Markup.ShouldContain("0,-52.0");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void NodeWithOnePortPerColumnKeepsOriginalBodyHeight()
    {
        // Arrange: a box-shaped node with the generic single in/out pair must keep the renderer's
        // pre-port 40-unit body — enabling ports must not shift any existing canvas.
        using var ctx = CreateContext();
        var model = new FakeCanvasModel
        {
            Nodes =
            [
                new FakeCanvasNode
                {
                    Id = "d1",
                    Label = "Store",
                    NodeType = CanvasNodeTypes.ByName("DataStore"),
                    X = 100,
                    Y = 100,
                    Ports = [InPort("A"), OutPort("B")],
                },
            ],
            Edges = [],
        };

        // Act
        var cut = ctx.Render<SvgCanvasRenderer>(p => p.Add(r => r.Model, model));

        // Assert
        var rect = cut.FindAll("rect").First(r => string.Equals(r.GetAttribute("rx"), "6", System.StringComparison.Ordinal));
        rect.GetAttribute("y").ShouldBe("-20.0");
        rect.GetAttribute("height").ShouldBe("40.0");
    }

    // ── 4. Port-anchored field-mapping edge geometry ───────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Ui")]
    public void SelfLoopFieldMappingEdgeRendersBetweenItsPorts()
    {
        // Arrange: the Map shape — a FieldMapping edge from in:{field} to out:{field} on ONE node.
        using var ctx = CreateContext();
        var model = new FakeCanvasModel
        {
            Nodes = [TransformNode(InPort("CustomerId"), OutPort("CustId"))],
            Edges =
            [
                new FakeCanvasEdge
                {
                    Id = "m1",
                    SourceNodeId = "t1",
                    TargetNodeId = "t1",
                    SourcePortId = "in:CustomerId",
                    TargetPortId = "out:CustId",
                    EdgeType = CanvasEdgeTypes.ByName("FieldMapping"),
                },
            ],
        };

        // Act
        var cut = ctx.Render<SvgCanvasRenderer>(p => p.Add(r => r.Model, model));

        // Assert: the mapping is drawn, anchored at the node-relative port offsets. The Transform's
        // port anchor half-width is 40 and each column holds one port, so the path runs from
        // (-40, 0) to (40, 0) inside the node's own translated group.
        var mappingPaths = cut.FindAll($"path[stroke='{FieldMappingStroke}']");
        mappingPaths.Count.ShouldBe(1, "Expected exactly one FieldMapping path");
        mappingPaths[0].GetAttribute("d").ShouldBe("M -40.0 0.0 C 0.0 0.0, 0.0 0.0, 40.0 0.0");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Ui")]
    public void EdgeAnchoredToPortTheNodeDoesNotExposeIsNotRendered()
    {
        // Arrange: the edge names in:Missing, but the node only exposes in:CustomerId. Anchoring it
        // at the node centre anyway would draw a mapping the model does not describe.
        using var ctx = CreateContext();
        var model = new FakeCanvasModel
        {
            Nodes = [TransformNode(InPort("CustomerId"), OutPort("CustId"))],
            Edges =
            [
                new FakeCanvasEdge
                {
                    Id = "m1",
                    SourceNodeId = "t1",
                    TargetNodeId = "t1",
                    SourcePortId = "in:Missing",
                    TargetPortId = "out:CustId",
                    EdgeType = CanvasEdgeTypes.ByName("FieldMapping"),
                },
            ],
        };

        // Act
        var cut = ctx.Render<SvgCanvasRenderer>(p => p.Add(r => r.Model, model));

        // Assert
        cut.FindAll($"path[stroke='{FieldMappingStroke}']").ShouldBeEmpty(
            "An edge whose port anchor cannot be resolved must not be drawn at a guessed position");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void NodeToNodeEdgeAnchorsToNamedPorts()
    {
        // Arrange: a Flow edge between two nodes, anchored to their generic out/in ports — the shape
        // PipelineDetailCanvasProjection builds when it chains a pipeline.
        using var ctx = CreateContext();
        var model = new FakeCanvasModel
        {
            Nodes =
            [
                new FakeCanvasNode
                {
                    Id = "n1", Label = "Src", NodeType = CanvasNodeTypes.ByName("DataStore"),
                    X = 0, Y = 0, Ports = [OutPort("X")],
                },
                new FakeCanvasNode
                {
                    Id = "n2", Label = "Dst", NodeType = CanvasNodeTypes.ByName("DataStore"),
                    X = 300, Y = 0, Ports = [InPort("Y")],
                },
            ],
            Edges =
            [
                new FakeCanvasEdge
                {
                    Id = "f1", SourceNodeId = "n1", TargetNodeId = "n2",
                    SourcePortId = "out:X", TargetPortId = "in:Y",
                    EdgeType = CanvasEdgeTypes.ByName("Flow"),
                },
            ],
        };

        // Act
        var cut = ctx.Render<SvgCanvasRenderer>(p => p.Add(r => r.Model, model));

        // Assert: anchored at the ports (n1's out at X+60, n2's in at X-60), not the node-level
        // ±70 fallback anchors.
        cut.FindAll("path[stroke='#06b6d4']")[0].GetAttribute("d")
            .ShouldBe("M 60.0 0.0 C 150.0 0.0, 150.0 0.0, 240.0 0.0");
    }

    // ── 5. Port-connect gesture ────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Ui")]
    public void ClickingSourcePortThenTargetPortConnectsWithPortIds()
    {
        // Arrange
        using var ctx = CreateContext();
        var editContext = CreateEditContext();
        var model = new FakeCanvasModel
        {
            Nodes = [TransformNode(InPort("CustomerId"), OutPort("CustId"))],
            Edges = [],
            RenderMode = CreateEditMode(),
            EditContext = editContext.Object,
        };

        var cut = ctx.Render<SvgCanvasRenderer>(p => p.Add(r => r.Model, model));

        // Act: click the in-port (source) then the out-port (target). Re-query between clicks —
        // the first click re-renders.
        cut.FindAll("circle")[0].Click();
        cut.FindAll("circle")[1].Click();

        // Assert: the port ids reach the edit context verbatim, as a FieldMapping edge. The
        // serializer reads the field names straight back out of these ids.
        editContext.Verify(
            c => c.Connect(
                "t1", "t1",
                It.Is<ICanvasEdgeType>(t => t.Name == "FieldMapping"),
                "in:CustomerId", "out:CustId",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void ClickingTheSamePortTwiceCancelsWithoutConnecting()
    {
        // Arrange
        using var ctx = CreateContext();
        var editContext = CreateEditContext();
        var model = new FakeCanvasModel
        {
            Nodes = [TransformNode(InPort("CustomerId"), OutPort("CustId"))],
            Edges = [],
            RenderMode = CreateEditMode(),
            EditContext = editContext.Object,
        };

        var cut = ctx.Render<SvgCanvasRenderer>(p => p.Add(r => r.Model, model));

        // Act
        cut.FindAll("circle")[0].Click();
        cut.FindAll("circle")[0].Click();

        // Assert
        editContext.Verify(
            c => c.Connect(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ICanvasEdgeType>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void PendingSourcePortIsHighlighted()
    {
        // Arrange
        using var ctx = CreateContext();
        var model = new FakeCanvasModel
        {
            Nodes = [TransformNode(InPort("CustomerId"), OutPort("CustId"))],
            Edges = [],
            RenderMode = CreateEditMode(),
            EditContext = CreateEditContext().Object,
        };

        var cut = ctx.Render<SvgCanvasRenderer>(p => p.Add(r => r.Model, model));

        // Act
        cut.FindAll("circle")[0].Click();

        // Assert: the pending source disc takes the connect highlight; its sibling does not.
        cut.FindAll("circle")[0].GetAttribute("fill").ShouldBe("#06b6d4");
        cut.FindAll("circle")[1].GetAttribute("fill").ShouldNotBe("#06b6d4");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Ui")]
    public void PortConnectIsInertInViewMode()
    {
        // Arrange: a view-only canvas still shows ports, but drawing an edge is an edit operation.
        using var ctx = CreateContext();
        var editContext = CreateEditContext();
        var model = new FakeCanvasModel
        {
            Nodes = [TransformNode(InPort("CustomerId"), OutPort("CustId"))],
            Edges = [],
            EditContext = editContext.Object,
        };

        var cut = ctx.Render<SvgCanvasRenderer>(p => p.Add(r => r.Model, model));

        // Act
        cut.FindAll("circle")[0].Click();
        cut.FindAll("circle")[1].Click();

        // Assert
        editContext.Verify(
            c => c.Connect(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ICanvasEdgeType>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
