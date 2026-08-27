using System;
using Bunit;
using Bunit.ComponentFactories;
using Fdw.Agents.Clients.Models;
using Fdw.Agents.Components.AgentActions;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using AgentActionsPage = Fdw.Agents.UI.Pages.Pages.AgentActionsPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Agents;

/// <summary>
/// Component tests for the FDW <c>AgentActions</c> queue page (Agents.UI.Pages). Relocated from
/// reference-ui's Agent/AgentActionsPageTests, which asserted these behaviours through the hosted
/// page; here they run directly against the page component with a seeded
/// <see cref="AgentActionContext"/> swapped in for the live <see cref="AgentActionProvider"/>.
/// Badge assertions target the page's CURRENT classes (badge b-ok/b-run/b-warn/b-fail/b-idle).
/// </summary>
[Trait("Category", "Ui")]
public sealed class AgentActionsPageContentTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private IRenderedComponent<AgentActionsPage> Render(AgentActionContext seed)
    {
        _ctx.ComponentFactories.Add(new ProviderStubFactory<AgentActionProvider, AgentActionContext>(seed));
        return _ctx.Render<AgentActionsPage>();
    }

    private static readonly Guid SampleId = Guid.Parse("6f1d2c3b-4a59-4e7f-8b21-9c0d5e6f7a80");

    private static AgentActionPayload Action(Guid? id = null, string method = "POST", string status = "Pending") => new()
    {
        Id = id ?? SampleId,
        AgentLabel = "GptAgent",
        UserId = "u1",
        Route = "/api/x",
        Method = method,
        Status = status,
        RequestedAt = DateTimeOffset.Now,
    };

    [Fact]
    public void RendersLoadingBranchWhenLoadingAndEmpty()
    {
        var cut = Render(new AgentActionContext { IsLoading = true });
        cut.Markup.ShouldContain("Loading");
    }

    [Fact]
    public void RendersEmptyWhenNoActions()
    {
        var cut = Render(new AgentActionContext());
        cut.Markup.ShouldContain("NO_PENDING_ACTIONS");
    }

    [Fact]
    public void RendersTableRows()
    {
        var cut = Render(new AgentActionContext { Actions = [Action(), Action(Guid.Parse("1b2c3d4e-5f60-4718-9a2b-3c4d5e6f7081"))] });
        cut.FindAll("tbody tr").Count.ShouldBe(2);
        cut.Markup.ShouldContain("GptAgent");
    }

    [Theory]
    [InlineData("POST", "b-ok")]
    [InlineData("PUT", "b-run")]
    [InlineData("PATCH", "b-warn")]
    [InlineData("DELETE", "b-fail")]
    [InlineData("GET", "b-idle")]
    public void RendersMethodBadge(string method, string fragment)
    {
        var cut = Render(new AgentActionContext { Actions = [Action(method: method)] });
        cut.Markup.ShouldContain(fragment);
    }

    [Theory]
    [InlineData("Pending", "b-warn")]
    [InlineData("Approved", "b-ok")]
    [InlineData("Denied", "b-fail")]
    [InlineData("Other", "b-idle")]
    public void RendersStatusBadge(string status, string fragment)
    {
        var cut = Render(new AgentActionContext { Actions = [Action(status: status)] });
        cut.Markup.ShouldContain(fragment);
    }

    [Fact]
    public void ReviewButtonOnlyForPending()
    {
        var cut = Render(new AgentActionContext { Actions = [Action(status: "Approved")] });
        cut.FindAll("button").Any(b => string.Equals(b.TextContent.Trim(), "Review", StringComparison.Ordinal)).ShouldBeFalse();
    }

    [Fact]
    public void ReviewButtonShownForPending()
    {
        var cut = Render(new AgentActionContext { Actions = [Action(status: "Pending")] });
        cut.FindAll("button").Any(b => string.Equals(b.TextContent.Trim(), "Review", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public async Task RefreshInvokesOnLoadActions()
    {
        var calls = 0;
        var cut = Render(new AgentActionContext { OnLoadActions = () => { calls++; return Task.CompletedTask; } });
        cut.FindAll("button").First(b => b.TextContent.Contains("Refresh", StringComparison.Ordinal)).Click();
        await Task.Yield();
        calls.ShouldBe(1);
    }

    [Fact]
    public void ReviewButtonNavigatesToReview()
    {
        var cut = Render(new AgentActionContext { Actions = [Action(status: "Pending")] });
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Review", StringComparison.Ordinal)).Click();
        nav.Uri.ShouldEndWith($"/review/{SampleId}");
    }

    public void Dispose() => _ctx.Dispose();
}
