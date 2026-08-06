using Bunit;
using Fdw.Data.Components.DataStores;
using Fdw.Data.UI.Pages.Pages;
using Fdw.Services.Data.Clients.Models;
using Fdw.UI.Components.Blazor.Tests.ConnInfra;

namespace Fdw.UI.Components.Blazor.Tests.Components.Data;

/// <summary>
/// Tests for the <see cref="DataStores"/> FDW list page. Relocated from reference-ui's
/// DataStoresPageTests: the deep loading/empty/table/delete assertions were reframed in the app to
/// a host smoke, and the equivalent (or stronger) coverage now runs here against the FDW page
/// rendered through a stubbed <see cref="DataStoreProvider"/>.
/// </summary>
[Trait("Category", "Ui")]
public sealed class DataStoresPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private void Swap(DataStoreContext? seed = null) =>
        _ctx.ComponentFactories.Add(new ProviderFactory<DataStoreProvider, DataStoreContext>(seed));

    private static DataStoreSummaryPayload Store(string name, string conn = "PROD_SQL", int paths = 2) =>
        new() { Id = Guid.NewGuid(), Name = name, ConnectionName = conn, PathCount = paths };

    [Fact]
    public void RendersLoadingSpinnerWhenLoadingAndEmpty()
    {
        Swap(new DataStoreContext { IsLoading = true });
        var cut = _ctx.Render<DataStores>();
        cut.FindAll(".spin").Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void RendersEmptyStateWhenNoStores()
    {
        Swap(new DataStoreContext());
        var cut = _ctx.Render<DataStores>();
        cut.Markup.ShouldContain("No DataStores configured");
    }

    [Fact]
    public void RendersTableWithStoreRows()
    {
        var list = new List<DataStoreSummaryPayload> { Store("Sales", "SQL_A", 3), Store("HR", "SQL_B", 1) };
        Swap(new DataStoreContext { DataStores = list, FilteredDataStores = list });
        var cut = _ctx.Render<DataStores>();
        cut.Markup.ShouldContain("Sales");
        cut.Markup.ShouldContain("HR");
        cut.Markup.ShouldContain("SQL_A");
        cut.FindAll("td.num").Any(c => c.TextContent.Contains("3", StringComparison.Ordinal)).ShouldBeTrue(); // PathCount column
        cut.FindAll("tbody tr").Count.ShouldBe(2);
    }

    [Fact]
    public void RendersNewDataStoreButton()
    {
        Swap(new DataStoreContext());
        var cut = _ctx.Render<DataStores>();
        cut.FindAll("button").Any(b => b.TextContent.Contains("New DataStore", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void DeleteButtonOpensConfirmModal()
    {
        var list = new List<DataStoreSummaryPayload> { Store("Sales") };
        Swap(new DataStoreContext { DataStores = list, FilteredDataStores = list });
        var cut = _ctx.Render<DataStores>();
        cut.FindAll("button.danger").First(b => b.InnerHtml.Contains("M19 7l", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("Confirm Delete");
        cut.Markup.ShouldContain("Sales");
    }

    [Fact]
    public void DeleteModalCancelCloses()
    {
        var list = new List<DataStoreSummaryPayload> { Store("Sales") };
        Swap(new DataStoreContext { DataStores = list, FilteredDataStores = list });
        var cut = _ctx.Render<DataStores>();
        cut.FindAll("button.danger").First(b => b.InnerHtml.Contains("M19 7l", StringComparison.Ordinal)).Click();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Cancel", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldNotContain("Confirm Delete");
    }

    [Fact]
    public async Task DeleteModalConfirmInvokesDelete()
    {
        string? deleted = null;
        var list = new List<DataStoreSummaryPayload> { Store("Sales") };
        Swap(new DataStoreContext
        {
            DataStores = list,
            FilteredDataStores = list,
            OnDeleteDataStore = n => { deleted = n; return Task.FromResult(true); }
        });
        var cut = _ctx.Render<DataStores>();
        cut.FindAll("button.danger").First(b => b.InnerHtml.Contains("M19 7l", StringComparison.Ordinal)).Click();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Delete", StringComparison.Ordinal)).Click();
        await Task.Yield();
        deleted.ShouldBe("Sales");
        cut.Markup.ShouldNotContain("Confirm Delete");
    }

    public void Dispose() => _ctx.Dispose();
}
