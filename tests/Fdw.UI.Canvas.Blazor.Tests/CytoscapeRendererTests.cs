using Bunit;
using TestContext = Bunit.BunitContext;
using Fdw.UI.Canvas.Blazor.Renderers.Cytoscape;
using Fdw.UI.Canvas.Blazor.Tests.Fakes;
using Shouldly;
using Xunit;

namespace Fdw.UI.Canvas.Blazor.Tests;

/// <summary>
/// bUnit tests for <c>CytoscapeRenderer</c>.
/// </summary>
public sealed class CytoscapeRendererTests
{
    // ── Shared helpers ─────────────────────────────────────────────────────────────

    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        // Why: CytoscapeRenderer uses JS interop (module import + render/dispose calls). Configure
        // bUnit in loose JS interop mode so all un-setup JS calls are silently ignored, including the
        // dynamic import() that loads cytoscape-interop.js on first render.
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    // ── 1. Host div renders for a small model ─────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void RendersHostDivForSmallModel()
    {
        // Arrange
        using var ctx = CreateContext();
        var model = new FakeCanvasModel();

        // Act
        var cut = ctx.Render<CytoscapeRenderer>(p => p
            .Add(r => r.Model, model));

        // Assert: the host div that Cytoscape mounts into must be present in the rendered output.
        var divs = cut.FindAll("div");
        divs.ShouldNotBeEmpty(
            "Expected at least one <div> to be rendered as the Cytoscape host container");
    }

    // ── 2. Empty model renders the empty-state message ────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void EmptyModelRendersEmptyStateMessage()
    {
        // Arrange
        using var ctx = CreateContext();
        var model = new FakeCanvasModel
        {
            Nodes = [],
            Edges = [],
        };

        // Act: must not throw.
        var cut = ctx.Render<CytoscapeRenderer>(p => p
            .Add(r => r.Model, model));

        // Assert: the empty-state overlay is shown when the model has no nodes.
        cut.Markup.ShouldContain("No nodes to display");
    }

    // ── 3. Non-empty model does not show empty-state message ──────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void NonEmptyModelDoesNotShowEmptyState()
    {
        // Arrange
        using var ctx = CreateContext();
        var model = new FakeCanvasModel();

        // Act
        var cut = ctx.Render<CytoscapeRenderer>(p => p
            .Add(r => r.Model, model));

        // Assert: the empty-state overlay must be absent when the model contains nodes.
        cut.Markup.ShouldNotContain("No nodes to display");
    }

    // ── 4. Empty model renders without error ──────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void EmptyModelRendersWithoutError()
    {
        // Arrange
        using var ctx = CreateContext();
        var model = new FakeCanvasModel
        {
            Nodes = [],
            Edges = [],
        };

        // Act: must not throw — CytoscapeRenderer guards against empty models at the overlay level;
        // the JS interop render call is skipped when _module is null (Loose interop returns null).
        var cut = ctx.Render<CytoscapeRenderer>(p => p
            .Add(r => r.Model, model));

        // Assert: component rendered (markup is not empty).
        cut.Markup.ShouldNotBeEmpty(
            "Expected rendered markup even for an empty canvas model");
    }
}
