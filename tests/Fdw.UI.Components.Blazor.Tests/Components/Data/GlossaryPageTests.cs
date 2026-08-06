using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Fdw.Services.Catalog.Clients.Models;
using Fdw.Services.Catalog.Components.Glossary;
using Fdw.UI.Components.Blazor.Tests.DataInfra;
using GlossaryPage = Fdw.Services.Catalog.UI.Pages.Pages.Glossary.Index;

namespace Fdw.UI.Components.Blazor.Tests.Components.Data;

/// <summary>
/// Page-level component tests for the FDW Glossary page
/// (<c>Fdw.Services.Catalog.UI.Pages/Pages/Glossary/Index.razor</c>).
/// Relocated from reference-ui's GlossaryPageTests. The outer GlossaryProvider is stubbed;
/// drives the stubbed <see cref="GlossaryContext"/> through its render branches: error banner,
/// loading state (no terms), empty state, and the populated term grid. Covers the refresh /
/// search / create-panel toggle / create-submit / delete actions, plus the create-validation
/// (blank term) branch.
/// </summary>
[Trait("Category", "Ui")]
public sealed class GlossaryPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private void Swap(GlossaryContext? seed = null) =>
        _ctx.ComponentFactories.Add(new ProviderFactory<GlossaryProvider, GlossaryContext>(seed));

    private static GlossaryTermPayload Term(string term, string def) => new()
    {
        Id = Guid.NewGuid(),
        Term = term,
        Definition = def,
    };

    [Fact]
    public void RendersErrorBannerWhenErrorMessagePresent()
    {
        Swap(new GlossaryContext { ErrorMessage = "glossary-boom" });
        var cut = _ctx.Render<GlossaryPage>();
        cut.Markup.ShouldContain("glossary-boom");
    }

    [Fact]
    public void RendersLoadingStateWhenLoadingAndNoTerms()
    {
        Swap(new GlossaryContext { IsLoading = true, Terms = [] });
        var cut = _ctx.Render<GlossaryPage>();
        // Why: current markup renders a "Loading terms..." card (no .animate-spin class).
        cut.Markup.ShouldContain("Loading terms...");
    }

    [Fact]
    public void RendersEmptyStateWhenNoTerms()
    {
        Swap(new GlossaryContext());
        var cut = _ctx.Render<GlossaryPage>();
        cut.Markup.ShouldContain("No glossary terms defined");
    }

    [Fact]
    public void RendersTermCardsWhenTermsPresent()
    {
        Swap(new GlossaryContext { Terms = [Term("PII", "Personally identifiable info"), Term("KPI", "Key perf indicator")] });
        var cut = _ctx.Render<GlossaryPage>();
        cut.Markup.ShouldContain("PII");
        cut.Markup.ShouldContain("Personally identifiable info");
        cut.Markup.ShouldContain("KPI");
    }

    [Fact]
    public async Task RefreshButtonInvokesOnRefresh()
    {
        var refreshed = 0;
        Swap(new GlossaryContext { Terms = [Term("X", "y")], OnRefresh = () => { refreshed++; return Task.CompletedTask; } });
        var cut = _ctx.Render<GlossaryPage>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Refresh", StringComparison.Ordinal)).Click();
        await Task.Yield();
        refreshed.ShouldBe(1);
    }

    [Fact]
    public async Task SearchButtonInvokesOnSearchWithQuery()
    {
        var queries = new List<string>();
        Swap(new GlossaryContext { Terms = [Term("X", "y")], OnSearch = q => { queries.Add(q); return Task.CompletedTask; } });
        var cut = _ctx.Render<GlossaryPage>();
        // Why: current markup search box is input.finput.mono with placeholder "Search glossary terms...".
        cut.FindAll("input").First(i => string.Equals(i.GetAttribute("placeholder"), "Search glossary terms...", StringComparison.Ordinal)).Input("kpi");
        cut.FindAll("button").First(b => b.TextContent.Contains("Search", StringComparison.Ordinal)).Click();
        await Task.Yield();
        queries.ShouldHaveSingleItem();
        queries[0].ShouldBe("kpi");
    }

    [Fact]
    public void NewTermButtonOpensCreatePanel()
    {
        Swap(new GlossaryContext { Terms = [Term("X", "y")] });
        var cut = _ctx.Render<GlossaryPage>();
        cut.FindAll("button").First(b => b.TextContent.Contains("New Term", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("Add Glossary Term");
    }

    [Fact]
    public void CreatePanelCancelClosesPanel()
    {
        Swap(new GlossaryContext { Terms = [Term("X", "y")] });
        var cut = _ctx.Render<GlossaryPage>();
        cut.FindAll("button").First(b => b.TextContent.Contains("New Term", StringComparison.Ordinal)).Click();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Cancel", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldNotContain("Add Glossary Term");
    }

    [Fact]
    public async Task CreatePanelSubmitBlankTermDoesNotInvokeOnCreate()
    {
        var created = 0;
        Swap(new GlossaryContext { Terms = [Term("X", "y")], OnCreate = _ => { created++; return Task.CompletedTask; } });
        var cut = _ctx.Render<GlossaryPage>();
        cut.FindAll("button").First(b => b.TextContent.Contains("New Term", StringComparison.Ordinal)).Click();
        // leave term blank
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Create", StringComparison.Ordinal)).Click();
        await Task.Yield();
        created.ShouldBe(0);
    }

    [Fact]
    public async Task CreatePanelSubmitValidInvokesOnCreateAndClosesPanel()
    {
        CreateGlossaryTermRequest? captured = null;
        Swap(new GlossaryContext { Terms = [Term("X", "y")], OnCreate = req => { captured = req; return Task.CompletedTask; } });
        var cut = _ctx.Render<GlossaryPage>();
        cut.FindAll("button").First(b => b.TextContent.Contains("New Term", StringComparison.Ordinal)).Click();
        cut.FindAll("input").First(i => string.Equals(i.GetAttribute("placeholder"), "Term name", StringComparison.Ordinal)).Change("PII");
        cut.FindAll("input").First(i => string.Equals(i.GetAttribute("placeholder"), "Term definition", StringComparison.Ordinal)).Change("Personal data");
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Create", StringComparison.Ordinal)).Click();
        await Task.Yield();
        captured.ShouldNotBeNull();
        captured!.Term.ShouldBe("PII");
        captured.Definition.ShouldBe("Personal data");
        cut.Markup.ShouldNotContain("Add Glossary Term");
    }

    [Fact]
    public async Task DeleteButtonInvokesOnDelete()
    {
        var t = Term("PII", "y");
        var deleted = new List<Guid>();
        Swap(new GlossaryContext { Terms = [t], OnDelete = id => { deleted.Add(id); return Task.CompletedTask; } });
        var cut = _ctx.Render<GlossaryPage>();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Delete", StringComparison.Ordinal)).Click();
        await Task.Yield();
        deleted.ShouldHaveSingleItem();
        deleted[0].ShouldBe(t.Id);
    }

    public void Dispose() => _ctx.Dispose();
}
