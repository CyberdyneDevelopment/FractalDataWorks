using Bunit;
using Bunit.ComponentFactories;
using Fdw.Services.Messaging.Clients.Models;
using Fdw.Services.Messaging.Components.Messaging;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using DetailPage = Fdw.UI.Pages.Messaging.Pages.MessageDetailPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Messaging;

/// <summary>
/// Component tests for the FDW <c>MessageDetail</c> page (Messaging.UI.Pages). Relocated from
/// reference-ui's Messaging/MessageDetailPageTests, which asserted these behaviours through the
/// hosted page; here they run directly against the page component with a seeded
/// <see cref="MessageDetailContext"/> swapped in for the live <c>MessageProvider</c>. Loading /
/// not-found text assertions target the page's CURRENT markup ("Loading…", "Not found").
/// </summary>
[Trait("Category", "Ui")]
public sealed class MessageDetailPageContentTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private IRenderedComponent<DetailPage> Render(MessageDetailContext seed)
    {
        _ctx.ComponentFactories.Add(new MessageProviderStubFactory(detail: seed));
        return _ctx.Render<DetailPage>(p => p.Add(d => d.Id, Guid.NewGuid()));
    }

    private static MessagePayload Msg(
        string subject = "Subj", string severity = "Normal", string? body = null,
        string? referenceId = null, string? resourceType = null, string? resourceId = null,
        string? actionUrl = null, string? actionType = null,
        DateTime? dismissedAt = null, DateTime? archivedAt = null) => new()
        {
            Id = Guid.NewGuid(),
            Subject = subject,
            Severity = severity,
            MessageType = "SystemNotification",
            Body = body,
            ReferenceId = referenceId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            ActionUrl = actionUrl,
            ActionType = actionType,
            DismissedAt = dismissedAt,
            ArchivedAt = archivedAt,
            CreatedAt = DateTime.UtcNow,
        };

    [Fact]
    public void RendersLoadingBranch()
    {
        var cut = Render(new MessageDetailContext { IsLoading = true });
        cut.Markup.ShouldContain("Loading");
    }

    [Fact]
    public void RendersNotFoundWhenMessageNull()
    {
        var cut = Render(new MessageDetailContext());
        cut.Markup.ShouldContain("Not found");
    }

    [Fact]
    public void RendersDetailCard()
    {
        var cut = Render(new MessageDetailContext { Message = Msg("Important") });
        cut.Markup.ShouldContain("Important");
    }

    [Theory]
    [InlineData("Critical", "b-fail")]
    [InlineData("High", "b-warn")]
    [InlineData("Normal", "b-idle")]
    [InlineData("Low", "b-run")]
    [InlineData("Other", "b-idle")]
    public void RendersSeverityBadge(string severity, string badgeClass)
    {
        var cut = Render(new MessageDetailContext { Message = Msg(severity: severity) });
        cut.Markup.ShouldContain(severity);
        cut.Markup.ShouldContain(badgeClass);
    }

    [Fact]
    public void RendersBodyWhenPresent()
    {
        var cut = Render(new MessageDetailContext { Message = Msg(body: "the body text") });
        cut.Markup.ShouldContain("the body text");
    }

    [Fact]
    public void RendersReferenceIdWithCopyButton()
    {
        var cut = Render(new MessageDetailContext { Message = Msg(referenceId: "REF-123") });
        cut.Markup.ShouldContain("REF-123");
    }

    [Fact]
    public void RendersResourceTypeAndId()
    {
        var cut = Render(new MessageDetailContext { Message = Msg(resourceType: "Connection", resourceId: "db1") });
        cut.Markup.ShouldContain("Connection");
        cut.Markup.ShouldContain("db1");
    }

    [Fact]
    public void RendersActionLinkWithActionTypeLabel()
    {
        var cut = Render(new MessageDetailContext { Message = Msg(actionUrl: "/go", actionType: "Approve") });
        cut.Markup.ShouldContain("/go");
        cut.Markup.ShouldContain("Approve");
    }

    [Fact]
    public void ActionLinkFallsBackToDefaultLabel()
    {
        var cut = Render(new MessageDetailContext { Message = Msg(actionUrl: "/go", actionType: null) });
        cut.Markup.ShouldContain("Take Action");
    }

    [Fact]
    public void DismissAndArchiveShownForActiveMessage()
    {
        var cut = Render(new MessageDetailContext { Message = Msg() });
        cut.FindAll("button").Any(b => b.TextContent.Contains("Dismiss", StringComparison.Ordinal)).ShouldBeTrue();
        cut.FindAll("button").Any(b => b.TextContent.Contains("Archive", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void DismissHiddenWhenAlreadyDismissed()
    {
        var cut = Render(new MessageDetailContext { Message = Msg(dismissedAt: DateTime.UtcNow) });
        cut.FindAll("button").Any(b => b.TextContent.Contains("Dismiss", StringComparison.Ordinal)).ShouldBeFalse();
    }

    [Fact]
    public async Task DismissInvokesOnDismissAndNavigates()
    {
        var dismissed = false;
        var cut = Render(new MessageDetailContext
        {
            Message = Msg(),
            OnDismiss = () => { dismissed = true; return Task.CompletedTask; },
        });
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Dismiss", StringComparison.Ordinal)).Click();
        await Task.Yield();
        dismissed.ShouldBeTrue();
        nav.Uri.ShouldEndWith("/messages");
    }

    [Fact]
    public async Task ArchiveInvokesOnArchiveAndNavigates()
    {
        var archived = false;
        var cut = Render(new MessageDetailContext
        {
            Message = Msg(),
            OnArchive = () => { archived = true; return Task.CompletedTask; },
        });
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Archive", StringComparison.Ordinal)).Click();
        await Task.Yield();
        archived.ShouldBeTrue();
        nav.Uri.ShouldEndWith("/messages");
    }

    public void Dispose() => _ctx.Dispose();
}
