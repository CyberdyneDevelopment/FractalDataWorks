using Bunit;
using Fdw.Operations.Clients.Models;
using Fdw.Operations.Components.Audit;
using Fdw.UI.Components.Blazor.Tests.ObsInfra;
using AuditPage = Fdw.Operations.UI.Pages.Pages.Audit;

namespace Fdw.UI.Components.Blazor.Tests.Components.Audit;

/// <summary>
/// Component tests for the FDW Audit (Execution History) page (<c>Pages/Audit.razor</c>). Relocated
/// from reference-ui's AuditPageTests; the page renders directly with its provider stubbed by a
/// seeded <see cref="AuditContext"/>. Assertions target the CURRENT markup (badge classes, footer
/// count format). Covers loading / empty branches, table rows + footer count, the state-badge
/// switch, in-flight duration formatting, the two filter selects, the Refresh action, and the
/// "Apply Filters" → OnFilterChanged wiring (previously documented as routing to OnRefresh).
/// </summary>
[Trait("Category", "Ui")]
public sealed class AuditPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private void SwapProvider(AuditContext? seed = null) =>
        _ctx.ComponentFactories.Add(new ProviderFactory<AuditProvider, AuditContext>(seed));

    private static ExecutionSummaryPayload Entry(string state = "Completed", string type = "Workflow", bool completed = true) => new()
    {
        Id = Guid.NewGuid(),
        Name = "nightly-load",
        ItemType = type,
        State = state,
        CreatedAt = DateTimeOffset.Now.AddMinutes(-5),
        CompletedAt = completed ? DateTimeOffset.Now : null,
    };

    [Fact]
    public void RendersLoadingWhenLoading()
    {
        SwapProvider(new AuditContext { IsLoading = true });
        var cut = _ctx.Render<AuditPage>();
        cut.Markup.ShouldContain("Loading execution history");
    }

    [Fact]
    public void RendersEmptyCardWhenNoEntries()
    {
        SwapProvider(new AuditContext());
        var cut = _ctx.Render<AuditPage>();
        cut.Markup.ShouldContain("No execution history found");
    }

    [Fact]
    public void RendersTableRowsAndFooterCount()
    {
        SwapProvider(new AuditContext { Entries = [Entry(), Entry()], TotalCount = 9 });
        var cut = _ctx.Render<AuditPage>();
        cut.FindAll("tbody tr").Count.ShouldBe(2);
        cut.Markup.ShouldContain("2 / 9");
    }

    [Theory]
    [InlineData("Completed", "badge b-ok")]
    [InlineData("Failed", "badge b-fail")]
    [InlineData("Running", "badge b-run")]
    [InlineData("Cancelled", "badge b-warn")]
    [InlineData("Pending", "badge b-idle")]
    public void RendersStateBadge(string state, string badgeFragment)
    {
        SwapProvider(new AuditContext { Entries = [Entry(state)], TotalCount = 1 });
        var cut = _ctx.Render<AuditPage>();
        cut.Markup.ShouldContain(badgeFragment);
    }

    [Fact]
    public void RendersInFlightDurationWhenNotCompleted()
    {
        SwapProvider(new AuditContext { Entries = [Entry(completed: false)], TotalCount = 1 });
        var cut = _ctx.Render<AuditPage>();
        cut.Markup.ShouldMatch(@"\d{2}:\d{2}:\d{2}");
    }

    [Fact]
    public void RendersFilterSelectsWithAllOptions()
    {
        SwapProvider(new AuditContext());
        var cut = _ctx.Render<AuditPage>();
        cut.Markup.ShouldContain("All States");
        cut.Markup.ShouldContain("All Types");
        cut.FindAll("select").Count.ShouldBe(2);
    }

    [Fact]
    public async Task RefreshInvokesOnRefresh()
    {
        var calls = 0;
        SwapProvider(new AuditContext { OnRefresh = () => { calls++; return Task.CompletedTask; } });
        var cut = _ctx.Render<AuditPage>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Refresh", StringComparison.Ordinal)).Click();
        await Task.Yield();
        calls.ShouldBe(1);
    }

    [Fact]
    public async Task ApplyFiltersInvokesOnFilterChanged()
    {
        var filterChangedCalls = 0;
        SwapProvider(new AuditContext
        {
            OnFilterChanged = (_, _) => { filterChangedCalls++; return Task.CompletedTask; },
        });
        var cut = _ctx.Render<AuditPage>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Apply Filters", StringComparison.Ordinal)).Click();
        await Task.Yield();
        filterChangedCalls.ShouldBe(1);
    }

    public void Dispose() => _ctx.Dispose();
}
