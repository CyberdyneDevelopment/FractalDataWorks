using Bunit;
using Bunit.ComponentFactories;
using Fdw.Agents.Clients.Models;
using Fdw.Agents.Components.AgentActions;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using ReviewAgentActionPage = Fdw.Agents.UI.Pages.Pages.ReviewAgentActionPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Agents;

/// <summary>
/// Component tests for the FDW <c>ReviewAgentAction</c> page (Agents.UI.Pages). Relocated from
/// reference-ui's Agent/ReviewAgentActionPageTests, which asserted these behaviours through the
/// hosted page; here they run directly against the page component with a seeded
/// <see cref="AgentActionContext"/> swapped in for the live <see cref="AgentActionProvider"/>.
/// Text assertions target the page's CURRENT markup (e.g. "ACTION_NOT_FOUND", "Agent Review Mode").
/// </summary>
[Trait("Category", "Ui")]
public sealed class ReviewAgentActionPageContentTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private IRenderedComponent<ReviewAgentActionPage> Render(AgentActionContext seed)
    {
        _ctx.ComponentFactories.Add(new ProviderStubFactory<AgentActionProvider, AgentActionContext>(seed));
        return _ctx.Render<ReviewAgentActionPage>(p => p.Add(r => r.ActionId, 7));
    }

    private static AgentActionPayload Action(
        string status = "Pending", string? body = null,
        DateTimeOffset? reviewedAt = null, string? reviewedBy = null) => new()
        {
            Id = Guid.CreateVersion7(),
            AgentLabel = "GptAgent",
            Route = "/api/x",
            Method = "POST",
            Status = status,
            RequestBody = body,
            ReviewedAt = reviewedAt,
            ReviewedBy = reviewedBy,
            RequestedAt = DateTimeOffset.Now,
        };

    [Fact]
    public void RendersLoadingBranch()
    {
        var cut = Render(new AgentActionContext { IsLoading = true });
        cut.Markup.ShouldContain("Loading");
    }

    [Fact]
    public void RendersNotFoundWhenCurrentActionNull()
    {
        var cut = Render(new AgentActionContext());
        cut.Markup.ShouldContain("ACTION_NOT_FOUND");
    }

    [Fact]
    public void RendersAlreadyReviewedPanelForNonPending()
    {
        var cut = Render(new AgentActionContext { CurrentAction = Action("Approved") });
        cut.Markup.ShouldContain("already been reviewed");
    }

    [Fact]
    public void RendersReviewerLineWhenReviewedAtPresent()
    {
        var cut = Render(new AgentActionContext
        {
            CurrentAction = Action("Denied", reviewedAt: DateTimeOffset.Now, reviewedBy: "admin"),
        });
        cut.Markup.ShouldContain("admin");
    }

    [Fact]
    public void RendersReviewModeForPending()
    {
        var cut = Render(new AgentActionContext { CurrentAction = Action("Pending", body: "{\"k\":1}") });
        cut.Markup.ShouldContain("Agent Review Mode");
        cut.Markup.ShouldContain("Request Body");
        cut.FindAll("button").Any(b => string.Equals(b.TextContent.Trim(), "Approve", StringComparison.Ordinal)).ShouldBeTrue();
        cut.FindAll("button").Any(b => string.Equals(b.TextContent.Trim(), "Deny", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void FormatsEmptyBodyAsPlaceholder()
    {
        var cut = Render(new AgentActionContext { CurrentAction = Action("Pending", body: null) });
        cut.Markup.ShouldContain("(empty)");
    }

    [Fact]
    public void FormatsInvalidJsonAsRaw()
    {
        var cut = Render(new AgentActionContext { CurrentAction = Action("Pending", body: "not-json") });
        cut.Markup.ShouldContain("not-json");
    }

    [Fact]
    public async Task ApproveSuccessNavigatesToQueue()
    {
        var cut = Render(new AgentActionContext
        {
            CurrentAction = Action("Pending"),
            OnApprove = _ => Task.FromResult(true),
        });
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Approve", StringComparison.Ordinal)).Click();
        await Task.Yield();
        nav.Uri.ShouldEndWith("/agent-actions");
    }

    [Fact]
    public void ApproveFailureShowsError()
    {
        var cut = Render(new AgentActionContext
        {
            CurrentAction = Action("Pending"),
            OnApprove = _ => Task.FromResult(false),
        });
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Approve", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to approve action"));
    }

    [Fact]
    public async Task DenySuccessNavigatesToQueue()
    {
        var cut = Render(new AgentActionContext
        {
            CurrentAction = Action("Pending"),
            OnDeny = _ => Task.FromResult(true),
        });
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Deny", StringComparison.Ordinal)).Click();
        await Task.Yield();
        nav.Uri.ShouldEndWith("/agent-actions");
    }

    [Fact]
    public void DenyFailureShowsError()
    {
        var cut = Render(new AgentActionContext
        {
            CurrentAction = Action("Pending"),
            OnDeny = _ => Task.FromResult(false),
        });
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Deny", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to deny action"));
    }

    public void Dispose() => _ctx.Dispose();
}
