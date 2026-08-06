using Bunit;
using Fdw.Operations.Clients.Models;
using Fdw.Operations.Components.Dataflow;
using Fdw.UI.Components.Blazor.Tests.PipeInfra;
using DataflowPage = Fdw.Operations.UI.Pages.Pages.Dataflow;

namespace Fdw.UI.Components.Blazor.Tests.Components.Dataflow;

/// <summary>
/// Component tests for the Dataflow overview page (Dataflow.razor). Relocated from reference-ui's
/// deep DataflowPageTests, which seeded a <see cref="DataflowContext"/> and asserted the rendered
/// graph markup. Here the page is rendered directly with its <c>DataflowProvider</c> swapped for a
/// stub yielding the seeded context.
/// </summary>
[Trait("Category", "Ui")]
[Collection(PageHostCollection.Name)]
public sealed class DataflowPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private static DataflowNodeResponse Node(string type, string label, string? category = null) =>
        new() { Id = $"{type}_{label}", NodeType = type, Label = label, Category = category };

    private static DataflowGraphPayload Graph(DataflowStatsResponse? stats, params DataflowNodeResponse[] nodes) =>
        new() { Stats = stats, Nodes = nodes };

    private IRenderedComponent<DataflowPage> RenderWith(DataflowContext context)
    {
        _ctx.RegisterPageInfrastructure();
        _ctx.ComponentFactories.Add(new ProviderFactory<DataflowProvider, DataflowContext>(context));
        return _ctx.Render<DataflowPage>();
    }

    [Fact]
    public void RendersLoadingPulseWhenLoading()
    {
        var cut = RenderWith(new DataflowContext { IsLoading = true });
        cut.Markup.ShouldContain("Loading dataflow graph", Case.Sensitive);
    }

    [Fact]
    public void RendersZeroStatsAndEmptyHintWhenGraphNull()
    {
        var cut = RenderWith(new DataflowContext { Graph = null });
        cut.Markup.ShouldContain("DataSets", Case.Sensitive);
        cut.Markup.ShouldContain("Connections", Case.Sensitive);
        cut.Markup.ShouldContain("No dataflow nodes found", Case.Sensitive);
    }

    [Fact]
    public void RendersSeededStatCounts()
    {
        var cut = RenderWith(new DataflowContext
        {
            Graph = Graph(new DataflowStatsResponse
            {
                DataSetCount = 7,
                DataStoreCount = 3,
                SourceCount = 5,
                ConnectionCount = 2,
            }),
        });
        cut.Markup.ShouldContain(">7<", Case.Sensitive);
        cut.Markup.ShouldContain(">3<", Case.Sensitive);
        cut.Markup.ShouldContain(">5<", Case.Sensitive);
        cut.Markup.ShouldContain(">2<", Case.Sensitive);
    }

    [Fact]
    public void RendersEmptyCardWhenGraphHasNoNodes()
    {
        var cut = RenderWith(new DataflowContext { Graph = Graph(new DataflowStatsResponse()) });
        cut.Markup.ShouldContain("No dataflow nodes found", Case.Sensitive);
        cut.Markup.ShouldContain("Configure DataSets and DataStores", Case.Sensitive);
    }

    [Fact]
    public void RendersNodeGroupsWithPerTypeCounts()
    {
        var cut = RenderWith(new DataflowContext
        {
            Graph = Graph(
                new DataflowStatsResponse(),
                Node("DataSet", "orders"),
                Node("DataSet", "customers"),
                Node("Connection", "primary")),
        });
        // Why: nodes group by NodeType; each group header shows a <span class="meta"> with its count.
        cut.Markup.ShouldContain("DataSet", Case.Sensitive);
        cut.Markup.ShouldContain("Connection", Case.Sensitive);
        cut.Markup.ShouldContain(">2<", Case.Sensitive);
        cut.Markup.ShouldContain(">1<", Case.Sensitive);
        cut.Markup.ShouldContain("orders", Case.Sensitive);
        cut.Markup.ShouldContain("customers", Case.Sensitive);
        cut.Markup.ShouldContain("primary", Case.Sensitive);
    }

    [Theory]
    [InlineData("Pipeline", "badge b-run")]
    [InlineData("DataSet", "badge b-run")]
    [InlineData("DataStore", "badge b-ok")]
    [InlineData("Connection", "badge b-warn")]
    [InlineData("Source", "badge b-idle")]
    public void RendersNodeTypeBadgeClass(string nodeType, string expectedClass)
    {
        // Why: GetNodeTypeBadge maps NodeType -> badge class. The old reference-ui test asserted
        // stale fragments (text-cyan-500/badge-running); the CURRENT page emits b-run/b-ok/b-warn/b-idle.
        var cut = RenderWith(new DataflowContext
        {
            Graph = Graph(new DataflowStatsResponse(), Node(nodeType, "n1")),
        });
        cut.Markup.ShouldContain(expectedClass, Case.Sensitive);
    }

    [Fact]
    public void RendersNodeCategoryWhenPresent()
    {
        var cut = RenderWith(new DataflowContext
        {
            Graph = Graph(new DataflowStatsResponse(), Node("DataSet", "orders", category: "Curated")),
        });
        cut.Markup.ShouldContain("Curated", Case.Sensitive);
    }

    [Fact]
    public void OmitsNodeCategoryWhenAbsent()
    {
        var cut = RenderWith(new DataflowContext
        {
            Graph = Graph(new DataflowStatsResponse(), Node("DataSet", "orders")),
        });
        cut.Markup.ShouldNotContain("font-size:11px;");
    }

    [Fact]
    public void RefreshButtonInvokesOnLoadGraph()
    {
        var loaded = false;
        var cut = RenderWith(new DataflowContext
        {
            Graph = Graph(new DataflowStatsResponse()),
            OnLoadGraph = () => { loaded = true; return Task.CompletedTask; },
        });
        cut.FindAll("button").First(b => b.TextContent.Contains("Refresh", StringComparison.Ordinal)).Click();
        loaded.ShouldBeTrue();
    }

    [Fact]
    public void RendersErrorMessageWhenPresent()
    {
        // Why: the CURRENT page DOES render ctx.ErrorMessage (Dataflow.razor lines 24-26) — the old
        // reference-ui "documented bug" where the error was swallowed no longer applies here.
        var cut = RenderWith(new DataflowContext { ErrorMessage = "graph load failed" });
        cut.Markup.ShouldContain("graph load failed", Case.Sensitive);
    }

    public void Dispose() => _ctx.Dispose();
}
