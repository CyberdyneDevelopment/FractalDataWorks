using Bunit;
using Fdw.Services.Quality.Clients.Models;
using Fdw.Services.Quality.Components.QualityRules;
using Fdw.Services.Quality.UI.Pages.Pages.Quality;
using Fdw.UI.Components.Blazor.Tests.ObsInfra;

namespace Fdw.UI.Components.Blazor.Tests.Components.Quality;

/// <summary>
/// Component tests for the FDW Quality Rules page (<c>Pages/Quality/Rules.razor</c>). Relocated from
/// reference-ui's QualityRulesPageTests; the page renders directly with its provider stubbed by a
/// seeded <see cref="QualityRuleContext"/>. Assertions target the CURRENT markup (badge classes,
/// loading class). The reference-ui suite documented a missing edit affordance and an unwired
/// RuleType field — both are now present in the page and verified here (create flows RuleType +
/// DataSetName through to OnCreate; a per-row Edit button opens the edit panel).
/// </summary>
[Trait("Category", "Ui")]
public sealed class QualityRulesPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private void SwapProvider(QualityRuleContext? seed = null) =>
        _ctx.ComponentFactories.Add(new ProviderFactory<QualityRuleProvider, QualityRuleContext>(seed));

    private static QualityRuleContext Ctx(
        IReadOnlyList<QualityRuleSummaryPayload>? rules = null,
        bool isLoading = false,
        string? error = null,
        Func<CreateQualityRulePayload, Task>? onCreate = null,
        Func<Guid, Task>? onDelete = null,
        Func<Guid, Task>? onExecute = null,
        Func<Task>? onRefresh = null) => new()
        {
            Rules = rules ?? [],
            IsLoading = isLoading,
            ErrorMessage = error,
            OnCreate = onCreate ?? (_ => Task.CompletedTask),
            OnDelete = onDelete ?? (_ => Task.CompletedTask),
            OnExecute = onExecute ?? (_ => Task.CompletedTask),
            OnRefresh = onRefresh ?? (() => Task.CompletedTask),
        };

    [Fact]
    public void RendersLoadingSpinnerWhenLoadingAndNoRules()
    {
        SwapProvider(Ctx(isLoading: true));
        var cut = _ctx.Render<Rules>();
        cut.Find(".loadwrap .spin").ShouldNotBeNull();
    }

    [Fact]
    public void RendersEmptyCardWhenNoRules()
    {
        SwapProvider(Ctx());
        var cut = _ctx.Render<Rules>();
        cut.Markup.ShouldContain("No quality rules defined");
    }

    [Fact]
    public void RendersErrorBannerWhenErrorPresent()
    {
        SwapProvider(Ctx(error: "rule load failed"));
        var cut = _ctx.Render<Rules>();
        cut.Markup.ShouldContain("rule load failed");
    }

    [Fact]
    public void RendersTableRowsWhenRulesPresent()
    {
        SwapProvider(Ctx(rules:
        [
            new() { Id = Guid.NewGuid(), Name = "NotNull", Description = "no nulls", IsEnabled = true },
            new() { Id = Guid.NewGuid(), Name = "Range",   Description = "0..100",   IsEnabled = false },
        ]));
        var cut = _ctx.Render<Rules>();
        cut.Markup.ShouldContain("NotNull");
        cut.Markup.ShouldContain("Range");
        cut.FindAll("tbody tr").Count.ShouldBe(2);
    }

    [Fact]
    public void RendersEnabledBadgeForEnabledRule()
    {
        SwapProvider(Ctx(rules: [new() { Id = Guid.NewGuid(), Name = "A", IsEnabled = true }]));
        var cut = _ctx.Render<Rules>();
        cut.Markup.ShouldContain("Enabled");
        cut.Find(".badge.b-ok").ShouldNotBeNull();
    }

    [Fact]
    public void RendersDisabledBadgeForDisabledRule()
    {
        SwapProvider(Ctx(rules: [new() { Id = Guid.NewGuid(), Name = "A", IsEnabled = false }]));
        var cut = _ctx.Render<Rules>();
        cut.Markup.ShouldContain("Disabled");
        cut.Find(".badge.b-idle").ShouldNotBeNull();
    }

    [Fact]
    public void RefreshAndNewButtonsDisabledWhenLoading()
    {
        SwapProvider(Ctx(rules: [new() { Id = Guid.NewGuid(), Name = "A" }], isLoading: true));
        var cut = _ctx.Render<Rules>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Refresh", StringComparison.Ordinal)).HasAttribute("disabled").ShouldBeTrue();
        cut.FindAll("button").First(b => b.TextContent.Contains("New Rule", StringComparison.Ordinal)).HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void NewRuleButtonShowsCreatePanel()
    {
        SwapProvider(Ctx());
        var cut = _ctx.Render<Rules>();
        cut.Markup.ShouldNotContain("Create Quality Rule");
        cut.FindAll("button").First(b => b.TextContent.Contains("New Rule", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("Create Quality Rule");
    }

    [Fact]
    public void CancelHidesCreatePanel()
    {
        SwapProvider(Ctx());
        var cut = _ctx.Render<Rules>();
        cut.FindAll("button").First(b => b.TextContent.Contains("New Rule", StringComparison.Ordinal)).Click();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Cancel", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldNotContain("Create Quality Rule");
    }

    [Fact]
    public async Task RefreshInvokesOnRefresh()
    {
        var calls = 0;
        SwapProvider(Ctx(onRefresh: () => { calls++; return Task.CompletedTask; }));
        var cut = _ctx.Render<Rules>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Refresh", StringComparison.Ordinal)).Click();
        await Task.Yield();
        calls.ShouldBe(1);
    }

    [Fact]
    public async Task CreateWithValidNameInvokesOnCreateAndClosesPanel()
    {
        CreateQualityRulePayload? sent = null;
        SwapProvider(Ctx(onCreate: r => { sent = r; return Task.CompletedTask; }));
        var cut = _ctx.Render<Rules>();
        cut.FindAll("button").First(b => b.TextContent.Contains("New Rule", StringComparison.Ordinal)).Click();

        cut.FindAll("input")[0].Change("Completeness");
        cut.FindAll("input")[1].Change("checks for nulls");
        cut.FindAll("input")[2].Change("Customers");
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Create", StringComparison.Ordinal)).Click();
        await Task.Yield();

        sent.ShouldNotBeNull();
        sent!.Name.ShouldBe("Completeness");
        sent.Description.ShouldBe("checks for nulls");
        sent.DataSetName.ShouldBe("Customers");
        cut.Markup.ShouldNotContain("Create Quality Rule");
    }

    [Fact]
    public async Task CreateWithWhitespaceNameDoesNotInvokeOnCreate()
    {
        var called = false;
        SwapProvider(Ctx(onCreate: _ => { called = true; return Task.CompletedTask; }));
        var cut = _ctx.Render<Rules>();
        cut.FindAll("button").First(b => b.TextContent.Contains("New Rule", StringComparison.Ordinal)).Click();
        cut.FindAll("input")[0].Change("   ");
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Create", StringComparison.Ordinal)).Click();
        await Task.Yield();
        called.ShouldBeFalse();
    }

    [Fact]
    public async Task CreateRequestCarriesSelectedRuleType()
    {
        CreateQualityRulePayload? sent = null;
        SwapProvider(Ctx(onCreate: r => { sent = r; return Task.CompletedTask; }));
        var cut = _ctx.Render<Rules>();
        cut.FindAll("button").First(b => b.TextContent.Contains("New Rule", StringComparison.Ordinal)).Click();
        cut.FindAll("input")[0].Change("Completeness");
        cut.Find("select").Change("NotNull");
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Create", StringComparison.Ordinal)).Click();
        await Task.Yield();

        sent.ShouldNotBeNull();
        sent!.RuleType.ShouldBe("NotNull");
    }

    [Fact]
    public async Task ExecuteButtonInvokesOnExecuteWithRuleId()
    {
        var id = Guid.NewGuid();
        Guid? executed = null;
        SwapProvider(Ctx(
            rules: [new() { Id = id, Name = "A" }],
            onExecute: g => { executed = g; return Task.CompletedTask; }));
        var cut = _ctx.Render<Rules>();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Execute", StringComparison.Ordinal)).Click();
        await Task.Yield();
        executed.ShouldBe(id);
    }

    [Fact]
    public async Task DeleteButtonInvokesOnDeleteWithRuleId()
    {
        var id = Guid.NewGuid();
        Guid? deleted = null;
        SwapProvider(Ctx(
            rules: [new() { Id = id, Name = "A" }],
            onDelete: g => { deleted = g; return Task.CompletedTask; }));
        var cut = _ctx.Render<Rules>();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Delete", StringComparison.Ordinal)).Click();
        await Task.Yield();
        deleted.ShouldBe(id);
    }

    [Fact]
    public void EditButtonOpensEditPanel()
    {
        SwapProvider(Ctx(rules: [new() { Id = Guid.NewGuid(), Name = "A", Description = "desc", IsEnabled = true }]));
        var cut = _ctx.Render<Rules>();
        cut.FindAll("button").Any(b => string.Equals(b.TextContent.Trim(), "Edit", StringComparison.Ordinal)).ShouldBeTrue();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Edit", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("Edit Quality Rule");
    }

    public void Dispose() => _ctx.Dispose();
}
