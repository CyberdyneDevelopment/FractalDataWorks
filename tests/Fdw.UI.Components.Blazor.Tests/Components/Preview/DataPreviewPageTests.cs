using Bunit;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Data.UI.Components;
using Fdw.Services.Data.Abstractions.Visualization;
using Fdw.UI.Components.Blazor.Tests.DataInfra;
using DataPreviewPage = Fdw.UI.Pages.Data.Pages.DataPreviewPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Preview;

/// <summary>
/// Page-level component tests for the FDW Data Preview page (<c>Data.UI.Pages/Pages/DataPreview.razor</c>).
/// Relocated from reference-ui's DataPreviewPageTests. The outer DataPreviewPageProvider is stubbed;
/// the nested QueryPanel + PreviewPanel are real param-only components. Render branches: DataStore /
/// DataSet mode-toggle active styling, error card, visualization toolbar (Columns&gt;0 &amp;&amp; !IsLoading),
/// viz-type buttons. Actions: set table mode, set dataset mode, viz-type select.
/// </summary>
[Trait("Category", "Ui")]
public sealed class DataPreviewPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private void SwapProvider(DataPreviewPageContext? seed = null) =>
        _ctx.ComponentFactories.Add(new ProviderFactory<DataPreviewPageProvider, DataPreviewPageContext>(seed));

    private sealed class TestVizType(int id, string name, string display)
        : VisualizationTypeBase(id, name, display, "icon", [])
    {
        public override bool CanVisualize(IReadOnlyList<string> columnTypes) => true;
        public override VisualizationConfig GetDefaultConfiguration() => new();
    }

    private static TestVizType Viz(string name, string display) => new(name.Length, name, display);

    [Fact]
    public void TableModeButtonActiveWhenTableMode()
    {
        SwapProvider(new DataPreviewPageContext { Mode = "Table" });
        var cut = _ctx.Render<DataPreviewPage>();
        var tableBtn = cut.FindAll("button").First(b => b.TextContent.Trim() == "DataStore");
        tableBtn.ClassList.ShouldContain("on");
    }

    [Fact]
    public void DataSetModeButtonActiveWhenNotTableMode()
    {
        SwapProvider(new DataPreviewPageContext { Mode = "DataSet" });
        var cut = _ctx.Render<DataPreviewPage>();
        var dsBtn = cut.FindAll("button").First(b => b.TextContent.Trim() == "DataSet");
        dsBtn.ClassList.ShouldContain("on");
    }

    [Fact]
    public void RendersErrorCardWhenErrorMessagePresent()
    {
        SwapProvider(new DataPreviewPageContext { LastResult = GenericResult.Failure(new GenericMessage("query exploded")) });
        var cut = _ctx.Render<DataPreviewPage>();
        cut.Markup.ShouldContain("query exploded");
    }

    [Fact]
    public void NoVisualizationToolbarWhenNoColumns()
    {
        SwapProvider(new DataPreviewPageContext { Columns = [] });
        var cut = _ctx.Render<DataPreviewPage>();
        cut.Markup.ShouldNotContain("Visualization");
    }

    [Fact]
    public void RendersVisualizationToolbarWhenColumnsPresent()
    {
        SwapProvider(new DataPreviewPageContext
        {
            Columns = ["a", "b"],
            IsLoading = false,
            SelectedVizType = "Table",
            AvailableVizTypes = [Viz("Table", "Table"), Viz("BarChart", "Bar Chart")],
        });
        var cut = _ctx.Render<DataPreviewPage>();
        cut.Markup.ShouldContain("Visualization");
        cut.Markup.ShouldContain("Bar Chart");
    }

    [Fact]
    public async Task TableModeButtonInvokesOnSetTableMode()
    {
        var called = false;
        SwapProvider(new DataPreviewPageContext
        {
            Mode = "DataSet",
            OnSetTableMode = () => { called = true; return Task.CompletedTask; },
        });
        var cut = _ctx.Render<DataPreviewPage>();
        cut.FindAll("button").First(b => b.TextContent.Trim() == "DataStore").Click();
        await Task.Yield();
        called.ShouldBeTrue();
    }

    [Fact]
    public async Task DataSetModeButtonInvokesOnSetDataSetMode()
    {
        var called = false;
        SwapProvider(new DataPreviewPageContext
        {
            Mode = "Table",
            OnSetDataSetMode = () => { called = true; return Task.CompletedTask; },
        });
        var cut = _ctx.Render<DataPreviewPage>();
        cut.FindAll("button").First(b => b.TextContent.Trim() == "DataSet").Click();
        await Task.Yield();
        called.ShouldBeTrue();
    }

    [Fact]
    public void VizTypeButtonInvokesOnVizTypeSelected()
    {
        string? selected = null;
        SwapProvider(new DataPreviewPageContext
        {
            Columns = ["a"],
            SelectedVizType = "Table",
            AvailableVizTypes = [Viz("BarChart", "Bar Chart")],
            OnVizTypeSelected = v => selected = v,
        });
        var cut = _ctx.Render<DataPreviewPage>();
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Bar Chart").Click();
        selected.ShouldBe("BarChart");
    }

    public void Dispose() => _ctx.Dispose();
}
