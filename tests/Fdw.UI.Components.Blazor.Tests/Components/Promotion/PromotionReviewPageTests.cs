using Bunit;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Web.Analytics.Clients.Models;
using Fdw.Web.Analytics.Components.PromotionReview;
using Fdw.UI.Components.Blazor.Tests.ObsInfra;
using ReviewPage = Fdw.UI.Pages.Operations.Pages.Promotions.PromotionReviewPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Promotion;

/// <summary>
/// Component tests for the FDW Promotion Review page (<c>Pages/Promotions/Review.razor</c>).
/// Relocated from reference-ui's PromotionReviewPageTests; the page renders directly with its
/// provider stubbed by a seeded <see cref="PromotionReviewContext"/> and the route <c>Id</c>
/// parameter supplied. Assertions target the CURRENT markup (badge classes). Covers loading,
/// not-found, error, the detail card, the status-badge switch, the conditional Approve/Reject
/// (Pending only), and the Approve/Reject actions.
/// </summary>
[Trait("Category", "Ui")]
public sealed class PromotionReviewPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private void SwapProvider(PromotionReviewContext? seed = null) =>
        _ctx.ComponentFactories.Add(new ProviderFactory<PromotionReviewProvider, PromotionReviewContext>(seed));

    private IRenderedComponent<ReviewPage> RenderReview() =>
        _ctx.Render<ReviewPage>(p => p.Add(r => r.Id, Guid.NewGuid()));

    private static PromotionPayload Req(string status = "Pending") => new()
    {
        Id = Guid.NewGuid(),
        Name = "Release42",
        SourceEnvironment = "Dev",
        TargetEnvironment = "Prod",
        Status = status,
    };

    [Fact]
    public void RendersLoadingWhenLoadingAndRequestNull()
    {
        SwapProvider(new PromotionReviewContext { IsLoading = true });
        var cut = RenderReview();
        cut.Markup.ShouldContain("Loading");
    }

    [Fact]
    public void RendersNotFoundWhenRequestNullAndNotLoading()
    {
        SwapProvider(new PromotionReviewContext());
        var cut = RenderReview();
        cut.Markup.ShouldContain("Promotion request not found");
    }

    [Fact]
    public void RendersErrorBanner()
    {
        SwapProvider(new PromotionReviewContext { LastResult = GenericResult.Failure(new GenericMessage("review failed")) });
        var cut = RenderReview();
        cut.Markup.ShouldContain("review failed");
    }

    [Fact]
    public void RendersDetailCardWhenRequestPresent()
    {
        SwapProvider(new PromotionReviewContext { Request = Req() });
        var cut = RenderReview();
        cut.Markup.ShouldContain("Release42");
        cut.Markup.ShouldContain("Dev");
        cut.Markup.ShouldContain("Prod");
    }

    [Theory]
    [InlineData("Approved", "b-ok")]
    [InlineData("Rejected", "b-fail")]
    [InlineData("Pending", "b-warn")]
    [InlineData("Other", "b-idle")]
    public void RendersStatusBadge(string status, string badgeClass)
    {
        SwapProvider(new PromotionReviewContext { Request = Req(status) });
        var cut = RenderReview();
        cut.Find($".badge.{badgeClass}").ShouldNotBeNull();
    }

    [Fact]
    public void ApproveRejectShownOnlyForPending()
    {
        SwapProvider(new PromotionReviewContext { Request = Req("Approved") });
        var cut = RenderReview();
        cut.FindAll("button").Any(b => string.Equals(b.TextContent.Trim(), "Approve", StringComparison.Ordinal)).ShouldBeFalse();
    }

    [Fact]
    public async Task ApproveInvokesOnApprove()
    {
        var approved = false;
        SwapProvider(new PromotionReviewContext
        {
            Request = Req("Pending"),
            OnApprove = () => { approved = true; return Task.CompletedTask; },
        });
        var cut = RenderReview();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Approve", StringComparison.Ordinal)).Click();
        await Task.Yield();
        approved.ShouldBeTrue();
    }

    [Fact]
    public async Task RejectInvokesOnReject()
    {
        var rejected = false;
        SwapProvider(new PromotionReviewContext
        {
            Request = Req("Pending"),
            OnReject = () => { rejected = true; return Task.CompletedTask; },
        });
        var cut = RenderReview();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Reject", StringComparison.Ordinal)).Click();
        await Task.Yield();
        rejected.ShouldBeTrue();
    }

    public void Dispose() => _ctx.Dispose();
}
