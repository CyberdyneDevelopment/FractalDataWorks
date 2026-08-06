using Bunit;
using Fdw.Operations.Clients.Models;
using Fdw.Operations.Components.Dataflow;
using Fdw.Operations.Components.Lineage;
using Fdw.UI.Components.Blazor.Tests.PipeInfra;
using LineagePage = Fdw.Operations.UI.Pages.Pages.Lineage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Lineage;

/// <summary>
/// Component tests for the system Lineage page (Lineage.razor), which nests a
/// <c>DataflowProvider</c> inside a <c>LineageProvider</c>. Relocated from reference-ui's deep
/// LineagePageTests: the page builds its graph from the SEEDED DataflowContext when "Show All" is
/// clicked. Both providers are swapped for stubs (LineageContext default, DataflowContext with a
/// seeded graph). The page constructs *ApiClient instances from IHttpClientFactory in
/// OnInitialized — RegisterPageInfrastructure supplies the no-op factory + ILoggerFactory.
/// </summary>
[Trait("Category", "Ui")]
[Collection(PageHostCollection.Name)]
public sealed class LineagePageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private static DataflowContext DataflowWithGraph(params (string Id, string Type, string Label)[] nodes) =>
        new()
        {
            Graph = new DataflowGraphPayload
            {
                Nodes = nodes.Select(n => new DataflowNodeResponse { Id = n.Id, NodeType = n.Type, Label = n.Label }).ToList(),
                Edges = [],
            },
            OnLoadGraph = () => Task.CompletedTask,
        };

    private IRenderedComponent<LineagePage> RenderWith(LineageContext lineage, DataflowContext dataflow)
    {
        _ctx.RegisterPageInfrastructure();
        _ctx.ComponentFactories.Add(new ProviderFactory<LineageProvider, LineageContext>(lineage));
        _ctx.ComponentFactories.Add(new ProviderFactory<DataflowProvider, DataflowContext>(dataflow));
        return _ctx.Render<LineagePage>();
    }

    private IRenderedComponent<LineagePage> RenderDefault() =>
        RenderWith(new LineageContext(), new DataflowContext());

    [Fact]
    public void RendersAllEntityTypeOptions()
    {
        var cut = RenderDefault();
        cut.Markup.ShouldContain(">Pipeline<", Case.Sensitive);
        cut.Markup.ShouldContain(">DataSet<", Case.Sensitive);
        cut.Markup.ShouldContain(">DataStore<", Case.Sensitive);
        cut.Markup.ShouldContain(">Connection<", Case.Sensitive);
        cut.Markup.ShouldContain(">Calculation<", Case.Sensitive);
    }

    [Fact]
    public void NameSelectDisabledWhenNoTypeSelected()
    {
        var cut = RenderDefault();
        // Why: the second <select> binds disabled to string.IsNullOrEmpty(_entityType); no type is
        // selected on first render so it must be disabled.
        cut.FindAll("select")[1].HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void ShowLineageDisabledWhenNoName()
    {
        var cut = RenderDefault();
        cut.FindAll("button").First(b => b.TextContent.Contains("Show Lineage", StringComparison.Ordinal))
            .HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void ShowAllButtonEnabled()
    {
        var cut = RenderDefault();
        cut.FindAll("button").First(b => b.TextContent.Contains("Show All", StringComparison.Ordinal))
            .HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void RendersLoadingOverlayWhenLineageProviderLoading()
    {
        var cut = RenderWith(new LineageContext { IsLoading = true }, new DataflowContext());
        cut.Markup.ShouldContain("Loading lineage", Case.Sensitive);
    }

    [Fact]
    public void RendersLoadingOverlayWhenDataflowProviderLoading()
    {
        var cut = RenderWith(new LineageContext(), new DataflowContext { IsLoading = true });
        cut.Markup.ShouldContain("Loading lineage", Case.Sensitive);
    }

    [Fact]
    public void RendersEmptyHintWhenNoNodes()
    {
        var cut = RenderDefault();
        cut.Markup.ShouldContain("Select an entity type and name to view lineage", Case.Sensitive);
    }

    [Fact]
    public void ShowAllRendersSeededGraphNodes()
    {
        var cut = RenderWith(new LineageContext(),
            DataflowWithGraph(("DataStore_warehouse", "DataStore", "warehouse")));
        cut.FindAll("button").First(b => b.TextContent.Contains("Show All", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("warehouse", Case.Sensitive);
        cut.Markup.ShouldNotContain("Select an entity type and name to view lineage");
    }

    [Fact]
    public void ShowAllTruncatesLongLabels()
    {
        var cut = RenderWith(new LineageContext(),
            DataflowWithGraph(("DataSet_x", "DataSet", "ThisIsAVeryLongLabelName")));
        cut.FindAll("button").First(b => b.TextContent.Contains("Show All", StringComparison.Ordinal)).Click();
        // Why: TruncateLabel cuts labels >14 chars to 12 chars + "..".
        cut.Markup.ShouldContain("ThisIsAVeryL..", Case.Sensitive);
    }

    [Theory]
    [InlineData("Connection", "conn-a")]
    [InlineData("DataStore", "store-a")]
    [InlineData("Pipeline", "pipe-a")]
    [InlineData("Calculation", "calc-a")]
    [InlineData("DataSet", "set-a")]
    [InlineData("Container", "cont-a")]
    [InlineData("Transformation", "xform-a")]
    [InlineData("Unknown", "other-a")]
    public void ShowAllRendersNodeOfEachType(string type, string label)
    {
        var cut = RenderWith(new LineageContext(), DataflowWithGraph(($"{type}_{label}", type, label)));
        cut.FindAll("button").First(b => b.TextContent.Contains("Show All", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain(label, Case.Sensitive);
    }

    [Fact]
    public void RendersEdgePathWithMarkerEnd()
    {
        var dataflow = new DataflowContext
        {
            Graph = new DataflowGraphPayload
            {
                Nodes =
                [
                    new DataflowNodeResponse { Id = "a", NodeType = "DataSet", Label = "a" },
                    new DataflowNodeResponse { Id = "b", NodeType = "DataStore", Label = "b" },
                ],
                Edges = [new DataflowEdgeResponse { Id = "e1", Source = "a", Target = "b", Label = "ReadsFrom" }],
            },
            OnLoadGraph = () => Task.CompletedTask,
        };
        var cut = RenderWith(new LineageContext(), dataflow);
        cut.FindAll("button").First(b => b.TextContent.Contains("Show All", StringComparison.Ordinal)).Click();
        cut.FindAll("path[marker-end]").ShouldNotBeEmpty();
    }

    [Fact]
    public void ClickingNodeOpensDetailPanel()
    {
        var cut = RenderWith(new LineageContext(),
            DataflowWithGraph(("DataStore_warehouse", "DataStore", "warehouse")));
        cut.FindAll("button").First(b => b.TextContent.Contains("Show All", StringComparison.Ordinal)).Click();
        cut.Find("g.cursor-move").Click();
        cut.Markup.ShouldContain("Open Detail Page", Case.Sensitive);
        cut.Markup.ShouldContain("Type", Case.Sensitive);
        cut.Markup.ShouldContain("Status", Case.Sensitive);
    }

    [Fact]
    public void ClickingSameNodeTwiceDeselects()
    {
        var cut = RenderWith(new LineageContext(),
            DataflowWithGraph(("DataStore_warehouse", "DataStore", "warehouse")));
        cut.FindAll("button").First(b => b.TextContent.Contains("Show All", StringComparison.Ordinal)).Click();
        cut.Find("g.cursor-move").Click();
        cut.Markup.ShouldContain("Open Detail Page", Case.Sensitive);
        cut.Find("g.cursor-move").Click();
        cut.Markup.ShouldNotContain("Open Detail Page");
    }

    [Fact]
    public void CloseButtonDeselectsNode()
    {
        var cut = RenderWith(new LineageContext(),
            DataflowWithGraph(("DataStore_warehouse", "DataStore", "warehouse")));
        cut.FindAll("button").First(b => b.TextContent.Contains("Show All", StringComparison.Ordinal)).Click();
        cut.Find("g.cursor-move").Click();
        // Why: the detail-panel card header hosts a btn-ghost close button that nulls _selectedNode.
        cut.Find(".card-h .btn-ghost").Click();
        cut.Markup.ShouldNotContain("Open Detail Page");
    }

    [Fact]
    public void DataSetNodeShowsDrillIntoFields()
    {
        var cut = RenderWith(new LineageContext(),
            DataflowWithGraph(("DataSet_orders", "DataSet", "orders")));
        cut.FindAll("button").First(b => b.TextContent.Contains("Show All", StringComparison.Ordinal)).Click();
        cut.Find("g.cursor-move").Click();
        cut.Markup.ShouldContain("Drill into fields", Case.Sensitive);
    }

    [Fact]
    public void NonDataSetNodeDoesNotShowDrillIntoFields()
    {
        var cut = RenderWith(new LineageContext(),
            DataflowWithGraph(("DataStore_warehouse", "DataStore", "warehouse")));
        cut.FindAll("button").First(b => b.TextContent.Contains("Show All", StringComparison.Ordinal)).Click();
        cut.Find("g.cursor-move").Click();
        cut.Markup.ShouldNotContain("Drill into fields");
    }

    [Fact]
    public void DefaultTransformUsesResetScale()
    {
        var cut = RenderDefault();
        cut.Markup.ShouldContain("scale(0.85)", Case.Sensitive);
    }

    [Fact]
    public void ZoomInChangesTransform()
    {
        var cut = RenderDefault();
        // Why: ZoomIn steps scale +0.1 from the 0.85 default -> 0.95.
        cut.FindAll("button")[^3].Click();
        cut.Markup.ShouldContain("scale(0.95)", Case.Sensitive);
    }

    [Fact]
    public void ZoomOutChangesTransform()
    {
        var cut = RenderDefault();
        // Why: ZoomOut steps scale -0.1 from the 0.85 default -> 0.75.
        cut.FindAll("button")[^2].Click();
        cut.Markup.ShouldContain("scale(0.75)", Case.Sensitive);
    }

    [Fact]
    public void ResetViewRestoresDefaultScale()
    {
        var cut = RenderDefault();
        cut.FindAll("button")[^3].Click();
        cut.Markup.ShouldContain("scale(0.95)", Case.Sensitive);
        cut.FindAll("button")[^1].Click();
        cut.Markup.ShouldContain("scale(0.85)", Case.Sensitive);
    }

    [Fact]
    public void RendersErrorMessageFromEitherProvider()
    {
        var cut = RenderWith(new LineageContext { ErrorMessage = "lineage boom" }, new DataflowContext());
        cut.Markup.ShouldContain("lineage boom", Case.Sensitive);
    }

    public void Dispose() => _ctx.Dispose();
}
