using System.Net.Http;
using Bunit;
using Fdw.Calculations.UI.Pages.Pages;
using Fdw.Data.Components.DataSets;
using Fdw.Services.Data.Clients;
using Fdw.UI.Components.Blazor.Tests.DataInfra;
using Fdw.UI.Pipelines.Clients.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.UI.Components.Blazor.Tests.Components.Data;

/// <summary>
/// Page-level component tests for the FDW <c>CalculatedDesigner</c> page
/// (<c>Fdw.Calculations.UI.Pages.Pages.CalculatedDesigner</c>).
/// Relocated from reference-ui's CalculatedDesignerPageTests. The outer
/// <see cref="CalculatedDataSetProvider"/> is stubbed; markup strings/selectors are
/// verified against the current razor. Render branches: loading (SyncFromContext
/// early-returns), loaded nodes/connections projected from InitialTasks/InitialConnections,
/// palette add-node, node selection + delete, properties-panel selection branch, and the
/// save-layout success / null-result / throws branches plus the back-button navigation.
/// </summary>
[Trait("Category", "Ui")]
public sealed class CalculatedDesignerPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private void SwapProvider(CalculatedDataSetContext? seed = null)
    {
        // Why: the current FDW CalculatedDesigner injects DataStoreApiClient (used only on
        // Source-node selection). Register a no-op handler-backed client so DI can satisfy the
        // page; none of these tests select a Source node, so no HTTP call is made.
        _ctx.Services.AddSingleton(new DataStoreApiClient(
            new HttpClient(new MockHttpHandler()) { BaseAddress = new Uri("http://localhost/") },
            NullLogger<DataStoreApiClient>.Instance));
        _ctx.ComponentFactories.Add(new ProviderFactory<CalculatedDataSetProvider, CalculatedDataSetContext>(seed));
    }

    private static TaskPayload NodeTask(string name, string type) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        TaskType = type,
        PositionX = 100,
        PositionY = 100,
        Configuration = new Dictionary<string, object?>(StringComparer.Ordinal),
    };

    // ── Loading branch ──────────────────────────────────────────────────────

    [Fact]
    public void LoadingRendersPaletteHeaderNoSelection()
    {
        // Why: when IsLoading, SyncFromContext returns early — no nodes projected.
        SwapProvider(new CalculatedDataSetContext { IsLoading = true });
        var cut = _ctx.Render<CalculatedDesigner>();
        cut.Markup.ShouldContain("OPERATIONS PALETTE", Case.Sensitive);
        cut.Markup.ShouldContain("NO_SELECTION_ACTIVE", Case.Sensitive);
    }

    // ── Loaded branch — nodes projected from context ────────────────────────

    [Fact]
    public void LoadedProjectsInitialTasksIntoNodes()
    {
        SwapProvider(new CalculatedDataSetContext
        {
            IsLoading = false,
            InitialTasks = [NodeTask("SourceA", "Source"), NodeTask("OutB", "Output")],
        });
        var cut = _ctx.Render<CalculatedDesigner>();
        cut.Markup.ShouldContain("SourceA", Case.Sensitive);
        cut.Markup.ShouldContain("OutB", Case.Sensitive);
    }

    [Fact]
    public void LoadedProjectsInitialConnectionsIntoPaths()
    {
        var t1 = NodeTask("A", "Source");
        var t2 = NodeTask("B", "Output");
        SwapProvider(new CalculatedDataSetContext
        {
            IsLoading = false,
            InitialTasks = [t1, t2],
            InitialConnections = [new TaskConnectionPayload { Id = Guid.NewGuid(), SourceTaskId = t1.Id, TargetTaskId = t2.Id }],
        });
        var cut = _ctx.Render<CalculatedDesigner>();
        // a connection renders an SVG <path>
        cut.FindAll("path").Any().ShouldBeTrue();
    }

    // ── Palette: add node ───────────────────────────────────────────────────

    [Fact]
    public void PaletteAddNodeAddsNodeToCanvas()
    {
        SwapProvider(new CalculatedDataSetContext { IsLoading = false });
        var cut = _ctx.Render<CalculatedDesigner>();
        // palette buttons render "> @op.ToUpper()" so the Filter chip reads "FILTER"
        cut.FindAll("button").First(b => b.TextContent.Contains("FILTER", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("Filter_0", Case.Sensitive);
    }

    // ── Node selection + properties panel ───────────────────────────────────

    [Fact]
    public void SelectNodeShowsPropertiesPanel()
    {
        SwapProvider(new CalculatedDataSetContext { IsLoading = false, InitialTasks = [NodeTask("NodeX", "Map")] });
        var cut = _ctx.Render<CalculatedDesigner>();
        // mousedown on a node group selects it (node rendered at translate(100, 100))
        cut.Find("svg g[transform^='translate(100']").MouseDown();
        cut.Markup.ShouldContain("Instance Name", Case.Sensitive);
        cut.Markup.ShouldContain("Delete Node", Case.Sensitive);
    }

    [Fact]
    public void DeleteSelectedRemovesNode()
    {
        SwapProvider(new CalculatedDataSetContext { IsLoading = false, InitialTasks = [NodeTask("NodeX", "Map")] });
        var cut = _ctx.Render<CalculatedDesigner>();
        cut.Find("svg g[transform^='translate(100']").MouseDown();
        cut.FindAll("button").First(b => b.TextContent.Contains("Delete Node", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("NO_SELECTION_ACTIVE", Case.Sensitive);
        cut.Markup.ShouldNotContain("NodeX", Case.Sensitive);
    }

    // ── Save layout branches ────────────────────────────────────────────────

    [Fact]
    public async Task SaveLayoutSuccessShowsSaved()
    {
        SwapProvider(new CalculatedDataSetContext
        {
            IsLoading = false,
            OnSave = (_, _) => Task.FromResult<Guid?>(Guid.NewGuid()),
        });
        var cut = _ctx.Render<CalculatedDesigner>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Save Layout", StringComparison.Ordinal)).Click();
        await Task.Yield();
        cut.Markup.ShouldContain("Saved", Case.Sensitive);
    }

    [Fact]
    public async Task SaveLayoutNullResultShowsContextError()
    {
        SwapProvider(new CalculatedDataSetContext
        {
            IsLoading = false,
            ErrorMessage = "save-failed",
            OnSave = (_, _) => Task.FromResult<Guid?>(null),
        });
        var cut = _ctx.Render<CalculatedDesigner>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Save Layout", StringComparison.Ordinal)).Click();
        await Task.Yield();
        cut.Markup.ShouldContain("save-failed", Case.Sensitive);
    }

    [Fact]
    public async Task SaveLayoutThrowsShowsFailedToSave()
    {
        SwapProvider(new CalculatedDataSetContext
        {
            IsLoading = false,
            OnSave = (_, _) => throw new InvalidOperationException("x"),
        });
        var cut = _ctx.Render<CalculatedDesigner>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Save Layout", StringComparison.Ordinal)).Click();
        await Task.Yield();
        cut.Markup.ShouldContain("Failed to save", Case.Sensitive);
    }

    [Fact]
    public void BackButtonNavigatesToDataSets()
    {
        SwapProvider(new CalculatedDataSetContext { IsLoading = false });
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var cut = _ctx.Render<CalculatedDesigner>();
        // the first ghost button is the back arrow → navigates to /datasets
        cut.Find("button.btn-ghost").Click();
        nav.Uri.ShouldEndWith("/datasets", Case.Sensitive);
    }

    public void Dispose() => _ctx.Dispose();
}
