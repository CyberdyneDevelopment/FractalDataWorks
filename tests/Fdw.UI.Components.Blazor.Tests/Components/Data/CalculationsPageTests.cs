using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Fdw.Calculations.Components.Calculations;
using Fdw.UI.Components.Blazor.Tests.DataInfra;
using Fdw.Web.Calculations.Clients.Models;
using CalculationsPage = Fdw.Calculations.UI.Pages.Pages.Calculations;

namespace Fdw.UI.Components.Blazor.Tests.Components.Data;

/// <summary>
/// Page-level component tests for the FDW Calculations list page
/// (<c>Calculations.UI.Pages/Pages/Calculations.razor</c>). Relocated from
/// reference-ui's CalculationsPageTests; the outer CalculationProvider is stubbed.
/// Render branches: loading / empty / loaded, the enabled vs disabled status badge.
/// Actions: the per-row delete button opens the Confirm Delete modal; Cancel closes it;
/// Delete invokes OnDeleteCalculation and closes the modal.
/// Markup has drifted from reference-ui: the page no longer has a "New Calculation"
/// button or a per-row edit button (only delete), and the loading branch renders a
/// "Loading calculations…" card rather than an animate-spin spinner. Those reference-ui
/// tests are re-targeted to current markup; assertions remain at least as rigorous.
/// </summary>
[Trait("Category", "Ui")]
public sealed class CalculationsPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private void Swap(CalculationContext? seed = null) =>
        _ctx.ComponentFactories.Add(new ProviderFactory<CalculationProvider, CalculationContext>(seed));

    private static CalculationSummaryPayload Calc(string name, bool enabled = true) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        TargetDataSet = "Customers",
        ResultFieldName = "total",
        IsEnabled = enabled,
    };

    [Fact]
    public void RendersLoadingCardWhenLoadingAndNoCalculations()
    {
        Swap(new CalculationContext { IsLoading = true, FilteredCalculations = [] });
        var cut = _ctx.Render<CalculationsPage>();
        cut.Markup.Contains("Loading calculations", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    public void RendersEmptyMessageWhenNoCalculations()
    {
        Swap(new CalculationContext());
        var cut = _ctx.Render<CalculationsPage>();
        cut.Markup.Contains("No calculations defined", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    public void RendersActiveBadgeForEnabledCalculation()
    {
        var c = Calc("Sum", enabled: true);
        Swap(new CalculationContext { Calculations = [c], FilteredCalculations = [c] });
        var cut = _ctx.Render<CalculationsPage>();
        cut.Markup.Contains("Active", StringComparison.Ordinal).ShouldBeTrue();
        cut.Markup.Contains("Sum", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    public void RendersDisabledBadgeForDisabledCalculation()
    {
        var c = Calc("Sum", enabled: false);
        Swap(new CalculationContext { Calculations = [c], FilteredCalculations = [c] });
        var cut = _ctx.Render<CalculationsPage>();
        cut.Markup.Contains("Disabled", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    public void DeleteButtonOpensConfirmModal()
    {
        var c = Calc("Sum");
        Swap(new CalculationContext { Calculations = [c], FilteredCalculations = [c] });
        var cut = _ctx.Render<CalculationsPage>();
        // Why: the row now has a single action button (delete) — index 0.
        cut.Find("tbody tr").QuerySelectorAll("button")[0].Click();
        cut.Markup.Contains("Confirm Delete", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    public void DeleteModalCancelClosesModal()
    {
        var c = Calc("Sum");
        Swap(new CalculationContext { Calculations = [c], FilteredCalculations = [c] });
        var cut = _ctx.Render<CalculationsPage>();
        cut.Find("tbody tr").QuerySelectorAll("button")[0].Click();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Cancel", StringComparison.Ordinal)).Click();
        cut.Markup.Contains("Confirm Delete", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteModalConfirmInvokesOnDeleteCalculationAndClosesModal()
    {
        var c = Calc("Sum");
        var deleted = new List<Guid>();
        Swap(new CalculationContext
        {
            Calculations = [c],
            FilteredCalculations = [c],
            OnDeleteCalculation = id => { deleted.Add(id); return Task.FromResult(true); },
        });
        var cut = _ctx.Render<CalculationsPage>();
        cut.Find("tbody tr").QuerySelectorAll("button")[0].Click();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Delete", StringComparison.Ordinal)).Click();
        await Task.Yield();
        deleted.ShouldHaveSingleItem();
        deleted[0].ShouldBe(c.Id);
        cut.Markup.Contains("Confirm Delete", StringComparison.Ordinal).ShouldBeFalse();
    }

    public void Dispose() => _ctx.Dispose();
}
