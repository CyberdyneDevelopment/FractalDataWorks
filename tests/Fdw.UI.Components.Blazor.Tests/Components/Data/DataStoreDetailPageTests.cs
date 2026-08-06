using Bunit;
using Fdw.Data.Components.DataStores;
using Fdw.Data.UI.Pages.Pages;
using Fdw.Services.Data.Clients.Models;
using Fdw.UI.Components.Blazor.Tests.ConnInfra;
using Fdw.UI.DrillDown;

namespace Fdw.UI.Components.Blazor.Tests.Components.Data;

/// <summary>
/// Tests for the <see cref="DataStoreDetail"/> FDW drill-down page. Relocated from reference-ui's
/// DataStoreDetailPageTests: the deep loading/error/not-found/overview/path/container/field/tree/
/// breadcrumb/import/sync assertions were reframed in the app to a host smoke, and the equivalent
/// (or stronger) coverage now runs here against the FDW page rendered through a stubbed
/// <see cref="DataStoreDetailProvider"/>.
/// </summary>
[Trait("Category", "Ui")]
public sealed class DataStoreDetailPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private void Swap(DataStoreDetailContext? seed = null) =>
        _ctx.ComponentFactories.Add(new ProviderFactory<DataStoreDetailProvider, DataStoreDetailContext>(seed));

    private static DataStoreDetailPayload StoreWith(params DataStorePathPayload[] paths) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Sales",
        ConnectionName = "PROD_SQL",
        StoreType = "SqlServer",
        IsActive = true,
        Paths = paths
    };

    private IRenderedComponent<DataStoreDetail> RenderDetail() =>
        _ctx.Render<DataStoreDetail>(p => p.Add(x => x.Name, "Sales"));

    [Fact]
    public void RendersLoadingSpinnerWhenDrillLoading()
    {
        Swap(new DataStoreDetailContext { ConfigurationContext = new ConfigurationDrillDownContext { IsLoading = true } });
        var cut = RenderDetail();
        cut.FindAll(".spin").Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void RendersErrorMessageWhenDrillError()
    {
        Swap(new DataStoreDetailContext { ConfigurationContext = new ConfigurationDrillDownContext { ErrorMessage = "load fail" } });
        var cut = RenderDetail();
        cut.Markup.ShouldContain("load fail");
    }

    [Fact]
    public void RendersNotFoundWhenDataStoreNull()
    {
        Swap(new DataStoreDetailContext { ConfigurationContext = new ConfigurationDrillDownContext(), DataStore = null });
        var cut = RenderDetail();
        cut.Markup.ShouldContain("DataStore not found");
    }

    [Fact]
    public void RendersActiveBadgeWhenActive()
    {
        Swap(new DataStoreDetailContext { DataStore = StoreWith() });
        var cut = RenderDetail();
        cut.Markup.ShouldContain("Active");
    }

    [Fact]
    public void RendersInactiveBadgeWhenInactive()
    {
        var store = StoreWith();
        store.IsActive = false;
        Swap(new DataStoreDetailContext { DataStore = store });
        var cut = RenderDetail();
        cut.Markup.ShouldContain("Inactive");
    }

    [Fact]
    public void RendersEmptyTreeWhenNoNodes()
    {
        Swap(new DataStoreDetailContext { DataStore = StoreWith() });
        var cut = RenderDetail();
        cut.Markup.ShouldContain("No paths discovered");
    }

    [Fact]
    public void RendersOverviewPanelWhenNoNodeSelected()
    {
        var store = StoreWith();
        store.DisplayName = "Sales Store";
        store.WriteMode = "Append";
        Swap(new DataStoreDetailContext { DataStore = store });
        var cut = RenderDetail();
        cut.Markup.ShouldContain("DataStore Overview");
        cut.Markup.ShouldContain("Sales Store");
        cut.Markup.ShouldContain("SqlServer");
        cut.Markup.ShouldContain("Append");
    }

    [Fact]
    public void RendersPathDetailWhenPathSelected()
    {
        var path = new DataStorePathPayload
        {
            Name = "P1",
            PhysicalPath = "dbo",
            PathType = "Schema",
            Description = "primary schema",
            Containers = [new DataStoreContainerPayload { Name = "Orders", ContainerType = "Table" }]
        };
        var drill = new ConfigurationDrillDownContext
        {
            SelectedNode = new DrillDownNode<object> { Label = "P1", NodeType = "Path" }
        };
        Swap(new DataStoreDetailContext { ConfigurationContext = drill, DataStore = StoreWith(path), SelectedPath = path });
        var cut = RenderDetail();
        cut.Markup.ShouldContain("Path: P1");
        cut.Markup.ShouldContain("primary schema");
        cut.Markup.ShouldContain("Orders");
        cut.Markup.ShouldContain("Containers");
    }

    [Fact]
    public void RendersContainerDetailWithFieldTable()
    {
        var container = new DataStoreContainerPayload
        {
            Name = "Orders",
            PhysicalName = "dbo.Orders",
            ContainerType = "Table",
            SurrogateKeyFields = ["OrderId"],
            NaturalKeyFields = ["OrderNo"],
            SupportedOperations = ["Read", "Write"],
            Fields =
            [
                new DataStoreFieldPayload { Name = "OrderId", NativeDataType = "int", IsKey = true, Ordinal = 1, Precision = 10, Scale = 0 },
                new DataStoreFieldPayload { Name = "Note", NativeDataType = "nvarchar", IsNullable = true, Ordinal = 2, MaxLength = 200 }
            ]
        };
        var drill = new ConfigurationDrillDownContext
        {
            SelectedNode = new DrillDownNode<object> { Label = "Orders", NodeType = "Container" }
        };
        Swap(new DataStoreDetailContext { ConfigurationContext = drill, DataStore = StoreWith(), SelectedContainer = container });
        var cut = RenderDetail();
        cut.Markup.ShouldContain("Container: Orders");
        cut.Markup.ShouldContain("dbo.Orders");
        cut.Markup.ShouldContain("OrderId"); // surrogate key + field row
        cut.Markup.ShouldContain("OrderNo"); // natural key
        cut.Markup.ShouldContain("PK");       // key column marker
        cut.Markup.ShouldContain("10,0");     // precision,scale
    }

    [Fact]
    public void RendersFieldDetailWhenFieldSelected()
    {
        var field = new DataStoreFieldPayload
        {
            Name = "Amount",
            NativeDataType = "decimal",
            IsNullable = false,
            IsKey = false,
            Ordinal = 5,
            MaxLength = null,
            Precision = 18,
            Scale = 2
        };
        var drill = new ConfigurationDrillDownContext
        {
            SelectedNode = new DrillDownNode<object> { Label = "Amount", NodeType = "Field" }
        };
        Swap(new DataStoreDetailContext { ConfigurationContext = drill, DataStore = StoreWith(), SelectedField = field });
        var cut = RenderDetail();
        cut.Markup.ShouldContain("Field: Amount");
        cut.Markup.ShouldContain("decimal");
        cut.Markup.ShouldContain("18"); // precision
        cut.Markup.ShouldContain("2");  // scale
    }

    [Fact]
    public void RendersTreeNodesFromDrillDown()
    {
        var node = new DrillDownNode<object> { Label = "dbo", Subtitle = "schema", IsLeaf = false, IsExpanded = false };
        Swap(new DataStoreDetailContext
        {
            DataStore = StoreWith(),
            ConfigurationContext = new ConfigurationDrillDownContext { Nodes = [node] }
        });
        var cut = RenderDetail();
        cut.Markup.ShouldContain("dbo");
        cut.Markup.ShouldContain("schema");
    }

    [Fact]
    public async Task TreeNodeSelectInvokesOnNodeSelected()
    {
        DrillDownNode<object>? selected = null;
        var node = new DrillDownNode<object> { Label = "dbo", IsLeaf = true };
        Swap(new DataStoreDetailContext
        {
            DataStore = StoreWith(),
            ConfigurationContext = new ConfigurationDrillDownContext
            {
                Nodes = [node],
                OnNodeSelected = n => selected = n
            }
        });
        var cut = RenderDetail();
        cut.FindAll("button").First(b => b.TextContent.Contains("dbo", StringComparison.Ordinal)).Click();
        await Task.Yield();
        selected.ShouldBe(node);
    }

    [Fact]
    public async Task TreeNodeToggleExpandInvokesOnToggleExpandForNonLeaf()
    {
        DrillDownNode<object>? toggled = null;
        var node = new DrillDownNode<object> { Label = "dbo", IsLeaf = false };
        Swap(new DataStoreDetailContext
        {
            DataStore = StoreWith(),
            ConfigurationContext = new ConfigurationDrillDownContext
            {
                Nodes = [node],
                OnToggleExpand = n => toggled = n
            }
        });
        var cut = RenderDetail();
        // The expand chevron is a <span> (with stopPropagation) carrying the M9 5l7 7 SVG path.
        cut.FindAll("span").First(s => s.InnerHtml.Contains("M9 5l7 7", StringComparison.Ordinal)).Click();
        await Task.Yield();
        toggled.ShouldBe(node);
    }

    [Fact]
    public async Task RefreshButtonInvokesOnRefresh()
    {
        var refreshed = false;
        Swap(new DataStoreDetailContext
        {
            DataStore = StoreWith(),
            ConfigurationContext = new ConfigurationDrillDownContext { OnRefresh = () => { refreshed = true; return Task.CompletedTask; } }
        });
        var cut = RenderDetail();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Refresh", StringComparison.Ordinal)).Click();
        await Task.Yield();
        refreshed.ShouldBeTrue();
    }

    [Fact]
    public async Task ImportSchemaButtonInvokesOnImportSchemaWithConnectionName()
    {
        string? imported = null;
        Swap(new DataStoreDetailContext
        {
            DataStore = StoreWith(),
            OnImportSchema = c => { imported = c; return Task.CompletedTask; }
        });
        var cut = RenderDetail();
        cut.FindAll("button").First(b => b.TextContent.Contains("Import Schema", StringComparison.Ordinal)).Click();
        await Task.Yield();
        imported.ShouldBe("PROD_SQL");
    }

    [Fact]
    public async Task SyncSchemaButtonInvokesOnSyncSchemaWithConnectionName()
    {
        string? synced = null;
        Swap(new DataStoreDetailContext
        {
            DataStore = StoreWith(),
            OnSyncSchema = c => { synced = c; return Task.CompletedTask; }
        });
        var cut = RenderDetail();
        cut.FindAll("button").First(b => b.TextContent.Contains("Sync Schema", StringComparison.Ordinal)).Click();
        await Task.Yield();
        synced.ShouldBe("PROD_SQL");
    }

    [Fact]
    public void BreadcrumbRendersCrumbButtonsWhenPathPresent()
    {
        var crumb = new DrillDownNode<object> { Label = "dbo" };
        Swap(new DataStoreDetailContext
        {
            DataStore = StoreWith(),
            ConfigurationContext = new ConfigurationDrillDownContext { BreadcrumbPath = [crumb] }
        });
        var cut = RenderDetail();
        cut.FindAll("button.btn-ghost").Any(b => b.TextContent.Contains("dbo", StringComparison.Ordinal)).ShouldBeTrue();
    }

    public void Dispose() => _ctx.Dispose();
}
