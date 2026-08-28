using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Fdw.Data.Components.DataSets;
using Fdw.Services.Data.Clients.Models;
using Fdw.UI.Components.Blazor.Tests.DataInfra;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using DataSetsPage = Fdw.UI.Pages.Data.Pages.DataSetsPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Data;

/// <summary>
/// Page-level component tests for the FDW <c>DataSets</c> list page
/// (<c>Data.UI.Pages/Pages/DataSets.razor</c>). Relocated from reference-ui's
/// DataSetsPageTests. The outer <see cref="DataSetProvider"/> is stubbed via a seeded
/// <see cref="DataSetContext"/>. Render branches: loading spinner / empty card / loaded
/// table. Inputs: search box, category select. Actions: search change, category change,
/// New DataSet navigation, row navigation.
/// </summary>
/// <remarks>
/// The page was restyled from the original Tailwind utility markup to a semantic
/// class scheme. Selector drift applied while preserving meaning:
/// <c>.animate-spin</c> -&gt; <c>.spin</c>; row <c>button.group</c> -&gt; <c>tbody tr</c>;
/// abbreviation/category badge -&gt; <c>span.tpill</c> (scoped per column);
/// secondary name span <c>span.w-32</c> -&gt; the Name <c>td.mut</c> (3rd column);
/// "+New" button text "New" -&gt; "New DataSet".
/// </remarks>
[Trait("Category", "Ui")]
public sealed class DataSetsPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private void Swap(DataSetContext? seed = null) =>
        _ctx.ComponentFactories.Add(new ProviderFactory<DataSetProvider, DataSetContext>(seed));

    private static DataSetSummaryPayload Ds(
        string name,
        string? display = null,
        string? abbr = null,
        string? category = null,
        string? description = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        DisplayName = display,
        Abbreviation = abbr,
        Category = category ?? string.Empty,
        Description = description ?? string.Empty,
    };

    // -- Render branch: loading ------------------------------------------------

    [Fact]
    public void RendersLoadingSpinnerWhenLoadingAndNoDataSets()
    {
        Swap(new DataSetContext { IsLoading = true, DataSets = [] });
        var cut = _ctx.Render<DataSetsPage>();
        cut.Find(".spin").ShouldNotBeNull();
    }

    [Fact]
    public void DoesNotRenderSpinnerWhenLoadingButDataSetsPresent()
    {
        var ds = Ds("Customers");
        Swap(new DataSetContext { IsLoading = true, DataSets = [ds], FilteredDataSets = [ds] });
        var cut = _ctx.Render<DataSetsPage>();
        cut.FindAll(".spin").Count.ShouldBe(0);
        cut.Markup.ShouldContain("Customers", Case.Sensitive);
    }

    // -- Render branch: empty --------------------------------------------------

    [Fact]
    public void RendersEmptyMessageWhenNoDataSets()
    {
        Swap(new DataSetContext());
        var cut = _ctx.Render<DataSetsPage>();
        cut.Markup.ShouldContain("No DataSets configured.", Case.Sensitive);
    }

    // -- Render branch: loaded list --------------------------------------------

    [Fact]
    public void RendersDataSetRowsForFilteredDataSets()
    {
        var a = Ds("Customers", display: "Customer Master", abbr: "CUST", category: "Core", description: "All customers");
        var b = Ds("Orders", display: "Orders", abbr: "ORD", category: "Sales", description: "Order lines");
        Swap(new DataSetContext { DataSets = [a, b], FilteredDataSets = [a, b] });
        var cut = _ctx.Render<DataSetsPage>();

        cut.Markup.ShouldContain("Customer Master", Case.Sensitive);
        cut.Markup.ShouldContain("CUST", Case.Sensitive);
        cut.Markup.ShouldContain("Core", Case.Sensitive);
        cut.Markup.ShouldContain("All customers", Case.Sensitive);
        // One navigable row per filtered dataset (the header bar lives outside the table body).
        cut.FindAll("table.tbl tbody tr").Count.ShouldBe(2);
    }

    [Fact]
    public void RowHidesAbbreviationBadgeWhenAbbreviationBlank()
    {
        var ds = Ds("NoAbbr", abbr: null);
        Swap(new DataSetContext { DataSets = [ds], FilteredDataSets = [ds] });
        var cut = _ctx.Render<DataSetsPage>();
        // The abbreviation badge is the tpill in the first column; absent when blank.
        var abbrCell = cut.Find("table.tbl tbody tr td:first-child");
        abbrCell.QuerySelectorAll("span.tpill").Length.ShouldBe(0);
    }

    [Fact]
    public void RowFallsBackToNameWhenDisplayNameBlank()
    {
        var ds = Ds("RawName", display: null);
        Swap(new DataSetContext { DataSets = [ds], FilteredDataSets = [ds] });
        var cut = _ctx.Render<DataSetsPage>();
        cut.Markup.ShouldContain("RawName", Case.Sensitive);
    }

    [Fact]
    public void RowHidesNameColumnWhenDisplayNameEqualsName()
    {
        var ds = Ds("Same", display: "Same");
        Swap(new DataSetContext { DataSets = [ds], FilteredDataSets = [ds] });
        var cut = _ctx.Render<DataSetsPage>();
        var nameCol = cut.Find("table.tbl tbody tr td:nth-child(3)");
        nameCol.ShouldNotBeNull();
        string.Equals(nameCol.TextContent.Trim(), string.Empty, StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    public void RowHidesCategoryPillWhenCategoryBlank()
    {
        var ds = Ds("NoCat", category: null);
        Swap(new DataSetContext { DataSets = [ds], FilteredDataSets = [ds] });
        var cut = _ctx.Render<DataSetsPage>();
        // The category badge is the tpill in the 4th column; absent when blank.
        var catCell = cut.Find("table.tbl tbody tr td:nth-child(4)");
        catCell.QuerySelectorAll("span.tpill").Length.ShouldBe(0);
    }

    // -- Input: category filter <select> renders distinct categories -----------

    [Fact]
    public void CategoryFilterRendersAllCategoryOptions()
    {
        var ds = Ds("X", category: "Core");
        Swap(new DataSetContext
        {
            DataSets = [ds],
            FilteredDataSets = [ds],
            Categories = ["Core", "Sales", "Finance"],
        });
        var cut = _ctx.Render<DataSetsPage>();
        var options = cut.Find("select").QuerySelectorAll("option");
        // "All categories" + 3 distinct
        options.Length.ShouldBe(4);
        cut.Markup.ShouldContain("Finance", Case.Sensitive);
    }

    // -- Action: search input fires OnSearchStringChanged ----------------------

    [Fact]
    public void SearchInputInvokesOnSearchStringChanged()
    {
        var captured = new List<string>();
        Swap(new DataSetContext { OnSearchStringChanged = captured.Add });
        var cut = _ctx.Render<DataSetsPage>();
        cut.Find("input").Input("cust");
        captured.ShouldHaveSingleItem();
        captured[0].ShouldBe("cust");
    }

    // -- Action: category select fires OnCategoryFilterChanged -----------------

    [Fact]
    public void CategorySelectInvokesOnCategoryFilterChanged()
    {
        var captured = new List<string>();
        Swap(new DataSetContext
        {
            Categories = ["Core"],
            OnCategoryFilterChanged = captured.Add,
        });
        var cut = _ctx.Render<DataSetsPage>();
        cut.Find("select").Change("Core");
        captured.ShouldHaveSingleItem();
        captured[0].ShouldBe("Core");
    }

    // -- Action: New DataSet navigates to /datasets/new ------------------------

    [Fact]
    public void NewButtonNavigatesToNewDataSet()
    {
        Swap(new DataSetContext());
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var cut = _ctx.Render<DataSetsPage>();
        cut.FindAll("button").First(b => b.TextContent.Contains("New", StringComparison.Ordinal)).Click();
        nav.Uri.ShouldEndWith("/datasets/new");
    }

    // -- Action: row click navigates to /datasets/{name} -----------------------

    [Fact]
    public void RowClickNavigatesToDataSetDetail()
    {
        var ds = Ds("Customers");
        Swap(new DataSetContext { DataSets = [ds], FilteredDataSets = [ds] });
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var cut = _ctx.Render<DataSetsPage>();
        cut.Find("table.tbl tbody tr").Click();
        nav.Uri.ShouldEndWith("/datasets/Customers");
    }

    public void Dispose() => _ctx.Dispose();
}
