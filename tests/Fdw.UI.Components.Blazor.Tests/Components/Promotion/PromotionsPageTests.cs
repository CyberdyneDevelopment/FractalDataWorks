using Bunit;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Web.Analytics.Clients.Models;
using Fdw.Web.Analytics.Components.Promotions;
using Fdw.UI.Components.Blazor.Tests.ObsInfra;
using Index = Fdw.UI.Pages.Operations.Pages.Promotions.PromotionsIndexPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Promotion;

/// <summary>
/// Component tests for the FDW Promotions index page (<c>Pages/Promotions/Index.razor</c>).
/// Relocated from reference-ui's PromotionsPageTests; the page renders directly with its provider
/// stubbed by a seeded <see cref="PromotionContext"/>. Assertions target the CURRENT markup (badge
/// classes, "New promotion" button text). Covers loading / empty / error branches, table rows, the
/// status-badge switch, the create panel toggle, the whitespace guard, and the Approve/Reject/Refresh
/// actions. The "Promotion create drops Name" report is NOT reproducible here — Index binds the name
/// through to OnCreate (pinned by <see cref="CreateForwardsAllThreeFields"/>).
/// </summary>
[Trait("Category", "Ui")]
public sealed class PromotionsPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private void SwapProvider(PromotionContext? seed = null) =>
        _ctx.ComponentFactories.Add(new ProviderFactory<PromotionProvider, PromotionContext>(seed));

    private static PromotionContext Ctx(
        IReadOnlyList<PromotionPayload>? requests = null,
        bool isLoading = false,
        string? error = null,
        Func<CreatePromotionPayload, Task>? onCreate = null,
        Func<Guid, Task>? onApprove = null,
        Func<Guid, Task>? onReject = null,
        Func<Task<IGenericResult>>? onRefresh = null) => new()
        {
            Requests = requests ?? [],
            IsLoading = isLoading,
            LastResult = error is null ? null : GenericResult.Failure(new GenericMessage(error)),
            OnCreate = onCreate ?? (_ => Task.CompletedTask),
            OnApprove = onApprove ?? (_ => Task.CompletedTask),
            OnReject = onReject ?? (_ => Task.CompletedTask),
            OnRefresh = onRefresh ?? (() => Task.FromResult<IGenericResult>(GenericResult.Success())),
        };

    private static PromotionPayload Req(string name = "P1", string status = "Pending") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        SourceEnvironment = "Dev",
        TargetEnvironment = "Prod",
        Status = status,
    };

    [Fact]
    public void RendersLoadingCardWhenLoadingAndNoRequests()
    {
        SwapProvider(Ctx(isLoading: true));
        var cut = _ctx.Render<Index>();
        cut.Markup.ShouldContain("Loading promotions");
    }

    [Fact]
    public void RendersEmptyCardWhenNoRequests()
    {
        SwapProvider(Ctx());
        var cut = _ctx.Render<Index>();
        cut.Markup.ShouldContain("No promotion requests");
    }

    [Fact]
    public void RendersErrorBanner()
    {
        SwapProvider(Ctx(error: "promotion failed"));
        var cut = _ctx.Render<Index>();
        cut.Markup.ShouldContain("promotion failed");
    }

    [Fact]
    public void RendersTableRows()
    {
        SwapProvider(Ctx(requests: [Req("Alpha"), Req("Beta")]));
        var cut = _ctx.Render<Index>();
        cut.Markup.ShouldContain("Alpha");
        cut.Markup.ShouldContain("Beta");
        cut.FindAll("tbody tr").Count.ShouldBe(2);
    }

    [Theory]
    [InlineData("Approved", "b-ok")]
    [InlineData("Rejected", "b-fail")]
    [InlineData("Pending", "b-warn")]
    [InlineData("Cancelled", "b-idle")]
    public void RendersStatusBadge(string status, string badgeClass)
    {
        SwapProvider(Ctx(requests: [Req("P", status)]));
        var cut = _ctx.Render<Index>();
        cut.Find($".badge.{badgeClass}").ShouldNotBeNull();
    }

    [Fact]
    public void ApproveRejectButtonsOnlyForPending()
    {
        SwapProvider(Ctx(requests: [Req("P", "Approved")]));
        var cut = _ctx.Render<Index>();
        cut.FindAll("button").Any(b => string.Equals(b.TextContent.Trim(), "Approve", StringComparison.Ordinal)).ShouldBeFalse();
        cut.FindAll("button").Any(b => string.Equals(b.TextContent.Trim(), "Reject", StringComparison.Ordinal)).ShouldBeFalse();
    }

    [Fact]
    public void ApproveRejectButtonsShownForPending()
    {
        SwapProvider(Ctx(requests: [Req("P", "Pending")]));
        var cut = _ctx.Render<Index>();
        cut.FindAll("button").Any(b => string.Equals(b.TextContent.Trim(), "Approve", StringComparison.Ordinal)).ShouldBeTrue();
        cut.FindAll("button").Any(b => string.Equals(b.TextContent.Trim(), "Reject", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void NewPromotionTogglesCreatePanel()
    {
        SwapProvider(Ctx());
        var cut = _ctx.Render<Index>();
        cut.Markup.ShouldNotContain("Create Promotion Request");
        cut.FindAll("button").First(b => b.TextContent.Contains("New promotion", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("Create Promotion Request");
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Cancel", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldNotContain("Create Promotion Request");
    }

    [Fact]
    public async Task RefreshInvokesOnRefresh()
    {
        var calls = 0;
        SwapProvider(Ctx(onRefresh: () => { calls++; return Task.FromResult<IGenericResult>(GenericResult.Success()); }));
        var cut = _ctx.Render<Index>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Refresh", StringComparison.Ordinal)).Click();
        await Task.Yield();
        calls.ShouldBe(1);
    }

    [Fact]
    public async Task CreateForwardsAllThreeFields()
    {
        CreatePromotionPayload? sent = null;
        SwapProvider(Ctx(onCreate: r => { sent = r; return Task.CompletedTask; }));
        var cut = _ctx.Render<Index>();
        cut.FindAll("button").First(b => b.TextContent.Contains("New promotion", StringComparison.Ordinal)).Click();
        cut.FindAll("input")[0].Change("Release42");
        cut.FindAll("input")[1].Change("Development");
        cut.FindAll("input")[2].Change("Production");
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Create", StringComparison.Ordinal)).Click();
        await Task.Yield();

        sent.ShouldNotBeNull();
        sent!.Name.ShouldBe("Release42");
        sent.SourceEnvironment.ShouldBe("Development");
        sent.TargetEnvironment.ShouldBe("Production");
        cut.Markup.ShouldNotContain("Create Promotion Request");
    }

    [Fact]
    public async Task CreateWithWhitespaceNameDoesNotInvokeOnCreate()
    {
        var called = false;
        SwapProvider(Ctx(onCreate: _ => { called = true; return Task.CompletedTask; }));
        var cut = _ctx.Render<Index>();
        cut.FindAll("button").First(b => b.TextContent.Contains("New promotion", StringComparison.Ordinal)).Click();
        cut.FindAll("input")[0].Change("  ");
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Create", StringComparison.Ordinal)).Click();
        await Task.Yield();
        called.ShouldBeFalse();
    }

    [Fact]
    public async Task ApproveInvokesOnApproveWithId()
    {
        var req = Req("P", "Pending");
        Guid? approved = null;
        SwapProvider(Ctx(requests: [req], onApprove: g => { approved = g; return Task.CompletedTask; }));
        var cut = _ctx.Render<Index>();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Approve", StringComparison.Ordinal)).Click();
        await Task.Yield();
        approved.ShouldBe(req.Id);
    }

    [Fact]
    public async Task RejectInvokesOnRejectWithId()
    {
        var req = Req("P", "Pending");
        Guid? rejected = null;
        SwapProvider(Ctx(requests: [req], onReject: g => { rejected = g; return Task.CompletedTask; }));
        var cut = _ctx.Render<Index>();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Reject", StringComparison.Ordinal)).Click();
        await Task.Yield();
        rejected.ShouldBe(req.Id);
    }

    public void Dispose() => _ctx.Dispose();
}
