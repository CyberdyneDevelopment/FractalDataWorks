using System;
using System.Linq;
using Bunit;
using TestContext = Bunit.BunitContext;
using Fdw.UI.Abstractions.Canvas;
using Fdw.UI.Canvas.Blazor.Host;
using Fdw.UI.Canvas.Blazor.Tests.Fakes;
using Shouldly;
using Xunit;

namespace Fdw.UI.Canvas.Blazor.Tests;

/// <summary>
/// bUnit tests for <c>CanvasHost</c>.
/// </summary>
public sealed class CanvasHostTests
{
    // ── Shared helpers ─────────────────────────────────────────────────────────────

    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    // ── 1. Renderer TypeCollection is populated with the Svg renderer ───────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Ui")]
    public void RendererTypeCollectionContainsSvgRenderer()
    {
        // Arrange / Act: check the TypeCollection registry directly (no render needed).
        var all = CanvasRendererTypes.All().ToList();

        // Assert
        all.ShouldNotBeEmpty(
            "CanvasRendererTypes must have at least one registered renderer for CanvasHost to work");
        all.ShouldContain(
            r => string.Equals(r.Name, "Svg", StringComparison.Ordinal),
            "Expected the built-in 'Svg' renderer to be registered in CanvasRendererTypes");
    }

    // ── 2. Renderer dropdown lists the Svg option ──────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Ui")]
    public void RendererDropdownListsSvgRenderer()
    {
        // Arrange
        using var ctx = CreateContext();
        var model = new FakeCanvasModel();

        // Act
        var cut = ctx.Render<CanvasHost>(p => p
            .Add(h => h.Model, model));

        // Assert: the select dropdown contains an 'Svg' option.
        var options = cut.FindAll("select option");
        options.ShouldContain(
            o => string.Equals(o.GetAttribute("value"), "Svg", StringComparison.Ordinal),
            "Expected an 'Svg' renderer option in the renderer dropdown");
    }

    // ── 3. DynamicComponent mounts the SVG renderer for a small model ──────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void DynamicComponentMountsSvgRenderer()
    {
        // Arrange
        using var ctx = CreateContext();

        // Verify the descriptor actually carries a component type before testing the mount.
        var descriptor = CanvasRendererTypes.ByName("Svg");
        descriptor.ShouldNotBe(CanvasRendererTypes.NotFound,
            "SvgCanvasRendererType must be registered for DynamicComponent dispatch to work");
        descriptor.RenderComponentType.ShouldNotBeNull(
            "SvgCanvasRendererType must declare its RenderComponentType so CanvasHost can mount it");

        var model = new FakeCanvasModel();

        // Act
        var cut = ctx.Render<CanvasHost>(p => p
            .Add(h => h.Model, model));

        // Assert: the SvgCanvasRenderer renders an <svg> element as its canvas surface.
        var svgElements = cut.FindAll("svg");
        svgElements.ShouldNotBeEmpty(
            "Expected the SvgCanvasRenderer to mount and emit an <svg> element");
    }

    // ── 4. Switching to unknown renderer shows error, does not throw ───────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Ui")]
    public void SwitchingToUnknownRendererShowsErrorAndDoesNotThrow()
    {
        // Arrange
        using var ctx = CreateContext();
        var model = new FakeCanvasModel();

        // Act: render with the valid Svg renderer first; the CanvasHost starts on the Svg renderer.
        var cut = ctx.Render<CanvasHost>(p => p
            .Add(h => h.Model, model));

        // Simulate the dropdown being changed to an unregistered renderer name by invoking
        // the change handler via the select element.
        var select = cut.Find("select");
        Should.NotThrow(() => select.Change("UnknownRenderer"));

        // Assert: an error span (colour #ef4444) is rendered.
        var errorEls = cut.FindAll("[style*='ef4444']");
        errorEls.ShouldNotBeEmpty(
            "Expected an error message element after setting an unknown renderer name");
    }

    // ── 5. Empty model (0 nodes / 0 edges) renders without error ──────────────────

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

        // Act & Assert: no exception thrown; the SVG canvas still mounts.
        var cut = ctx.Render<CanvasHost>(p => p
            .Add(h => h.Model, model));

        // No error message should be present for a valid (but empty) model.
        var errorEls = cut.FindAll("[style*='ef4444']");
        errorEls.ShouldBeEmpty(
            "No error should appear when rendering an empty canvas model");

        // The SVG canvas is still rendered (no nodes = empty state displayed inside the SVG).
        var svgElements = cut.FindAll("svg");
        svgElements.ShouldNotBeEmpty(
            "The SVG renderer must mount even for an empty model");
    }
}
