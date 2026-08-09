using Bunit;
using Bunit.ComponentFactories;
using Fdw.Services.Messaging.Clients.Models;
using Fdw.Services.Messaging.Components.Messaging;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NewAccessRequestPage = Fdw.UI.Pages.Messaging.Pages.NewAccessRequestPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Messaging;

/// <summary>
/// Component tests for the FDW <c>NewAccessRequest</c> page (Messaging.UI.Pages). Relocated from
/// reference-ui's Messaging/NewAccessRequestPageTests, which asserted these behaviours through the
/// hosted page; here they run directly against the page component with a seeded
/// <see cref="AccessRequestListContext"/> swapped in for the live <c>MessageProvider</c>.
/// </summary>
[Trait("Category", "Ui")]
public sealed class NewAccessRequestPageContentTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private IRenderedComponent<NewAccessRequestPage> Render(AccessRequestListContext seed)
    {
        _ctx.ComponentFactories.Add(new MessageProviderStubFactory(access: seed));
        return _ctx.Render<NewAccessRequestPage>();
    }

    [Fact]
    public void RendersFormInputs()
    {
        var cut = Render(new AccessRequestListContext());
        cut.Markup.ShouldContain("New Access Request");
        cut.FindAll("input").Count.ShouldBeGreaterThanOrEqualTo(2);
        cut.FindAll("textarea").Count.ShouldBe(1);
    }

    [Fact]
    public async Task SubmitWithMissingFieldsShowsValidationError()
    {
        var created = false;
        var cut = Render(new AccessRequestListContext { OnCreate = _ => { created = true; return Task.CompletedTask; } });
        cut.FindAll("button").First(b => b.TextContent.Contains("Submit Request", StringComparison.Ordinal)).Click();
        await Task.Yield();
        cut.Markup.ShouldContain("Resource and Permission are required");
        created.ShouldBeFalse();
    }

    [Fact]
    public async Task SubmitWithValidFieldsInvokesOnCreateAndNavigates()
    {
        CreateAccessRequestModel? sent = null;
        var cut = Render(new AccessRequestListContext { OnCreate = m => { sent = m; return Task.CompletedTask; } });
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();

        cut.FindAll("input")[0].Change("Connection:Prod");
        cut.FindAll("input")[1].Change("Read");
        cut.Find("textarea").Change("for the audit");
        cut.FindAll("button").First(b => b.TextContent.Contains("Submit Request", StringComparison.Ordinal)).Click();
        await Task.Yield();

        sent.ShouldNotBeNull();
        sent!.RequestedResource.ShouldBe("Connection:Prod");
        sent.RequestedPermission.ShouldBe("Read");
        sent.Justification.ShouldBe("for the audit");
        nav.Uri.ShouldEndWith("/messages");
    }

    [Fact]
    public void CancelNavigatesToMessages()
    {
        var cut = Render(new AccessRequestListContext());
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Cancel", StringComparison.Ordinal)).Click();
        nav.Uri.ShouldEndWith("/messages");
    }

    public void Dispose() => _ctx.Dispose();
}
