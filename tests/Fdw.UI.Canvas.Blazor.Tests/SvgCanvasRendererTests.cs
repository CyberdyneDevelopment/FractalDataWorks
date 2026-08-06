using System;
using System.Linq;
using Bunit;
using TestContext = Bunit.BunitContext;
using Fdw.UI.Abstractions.Canvas;
using Fdw.UI.Canvas.Blazor.Renderers.Svg;
using Fdw.UI.Canvas.Blazor.Tests.Fakes;
using Shouldly;
using Xunit;

namespace Fdw.UI.Canvas.Blazor.Tests;

/// <summary>
/// bUnit tests for <c>SvgCanvasRenderer</c>.
/// </summary>
public sealed class SvgCanvasRendererTests
{
    // ── Shared helpers ─────────────────────────────────────────────────────────────

    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        // Why: SvgCanvasRenderer uses Blazor mouse/wheel event handlers. Configure bUnit in
        // loose JS interop mode so any un-setup JS call is silently ignored.
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    // ── 1. Renders expected number of node groups for the small model ───────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void RendersNodeGroupsForSmallModel()
    {
        // Arrange
        using var ctx = CreateContext();
        var model = new FakeCanvasModel();

        // Act
        var cut = ctx.Render<SvgCanvasRenderer>(p => p
            .Add(r => r.Model, model));

        // Assert: each node produces a <g> with a translate(...) transform inside the SVG.
        // The nodes in FakeCanvasModel have labels "Source" and "Target".
        var allG = cut.FindAll("g[transform]");
        // Why: count only the node groups (translate transform) — exclude the outer pan/zoom group
        // which carries a "translate(...) scale(...)" transform. The node groups have the pattern
        // "translate(X, Y)" without a scale component.
        var nodeGroups = allG
            .Where(g =>
            {
                var t = g.GetAttribute("transform") ?? string.Empty;
                return t.StartsWith("translate(", StringComparison.Ordinal)
                    && !t.Contains("scale", StringComparison.Ordinal);
            })
            .ToList();

        nodeGroups.Count.ShouldBe(2,
            "Expected one <g translate> per node (2 nodes in FakeCanvasModel)");
    }

    // ── 2. Node label text is present in the rendered SVG ─────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void NodeLabelTextIsPresentInSvg()
    {
        // Arrange
        using var ctx = CreateContext();
        var model = new FakeCanvasModel();

        // Act
        var cut = ctx.Render<SvgCanvasRenderer>(p => p
            .Add(r => r.Model, model));

        // Assert: the label "Source" from node-1 must appear in the SVG markup as a <text> element.
        // Why: SVG <text> is emitted via MarkupString (reserved Razor tag), so the text is directly
        // present in the DOM and queryable via markup assertions.
        var renderedMarkup = cut.Markup;
        // Why: single-arg string ShouldContain — the (expected, customMessage) overload binds to
        // Shouldly's IEnumerable<char> predicate overload and fails to compile.
        renderedMarkup.ShouldContain("Source");
        renderedMarkup.ShouldContain("Target");
    }

    // ── 3. Renders edge paths for the small model ──────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void RendersEdgePathsForSmallModel()
    {
        // Arrange
        using var ctx = CreateContext();
        var model = new FakeCanvasModel();

        // Act
        var cut = ctx.Render<SvgCanvasRenderer>(p => p
            .Add(r => r.Model, model));

        // Assert: each visible edge produces a <path> element with stroke attribute.
        // The FakeCanvasModel has 1 edge connecting node-1 → node-2.
        var paths = cut.FindAll("path[stroke]");
        paths.Count.ShouldBeGreaterThanOrEqualTo(1,
            "Expected at least one <path stroke> element for the Flow edge in FakeCanvasModel");
    }

    // ── 4. Empty model renders without error and shows empty-state message ──────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void EmptyModelRendersWithoutErrorAndShowsEmptyState()
    {
        // Arrange
        using var ctx = CreateContext();
        var model = new FakeCanvasModel
        {
            Nodes = [],
            Edges = [],
        };

        // Act: must not throw.
        var cut = ctx.Render<SvgCanvasRenderer>(p => p
            .Add(r => r.Model, model));

        // Assert: the SVG element is present (renderer mounted).
        var svgElements = cut.FindAll("svg");
        svgElements.ShouldNotBeEmpty(
            "Expected the SVG element to be rendered even for an empty canvas model");

        // The empty-state message is displayed.
        cut.Markup.ShouldContain("No nodes to display");
    }

    // ── 5. No node <g> elements when model has no nodes ───────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void EmptyModelRendersZeroNodeGroups()
    {
        // Arrange
        using var ctx = CreateContext();
        var model = new FakeCanvasModel
        {
            Nodes = [],
            Edges = [],
        };

        // Act
        var cut = ctx.Render<SvgCanvasRenderer>(p => p
            .Add(r => r.Model, model));

        // Assert: no node translate-groups.
        var nodeGroups = cut.FindAll("g[transform]")
            .Where(g =>
            {
                var t = g.GetAttribute("transform") ?? string.Empty;
                return t.StartsWith("translate(", StringComparison.Ordinal)
                    && !t.Contains("scale", StringComparison.Ordinal);
            })
            .ToList();

        nodeGroups.Count.ShouldBe(0,
            "Expected zero node groups when the model has no nodes");
    }
}
