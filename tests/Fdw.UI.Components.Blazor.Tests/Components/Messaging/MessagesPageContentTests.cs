using Bunit;
using Bunit.ComponentFactories;
using Fdw.Services.Messaging.Clients.Models;
using Fdw.Services.Messaging.Components.Messaging;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MessagesPage = Fdw.Services.Messaging.UI.Pages.Pages.Messages;

namespace Fdw.UI.Components.Blazor.Tests.Components.Messaging;

/// <summary>
/// Component tests for the FDW <c>Messages</c> list page (Messaging.UI.Pages). Relocated from
/// reference-ui's Messaging/MessagesPageTests, which asserted these behaviours through the hosted
/// page; here they run directly against the page component with a seeded
/// <see cref="MessageListContext"/> swapped in for the live <c>MessageProvider</c>. Severity/filter
/// assertions target the page's CURRENT markup (severity dot classes dot-red/dot-amber/…; chip
/// filter spans; select type/severity filters).
/// </summary>
[Trait("Category", "Ui")]
public sealed class MessagesPageContentTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private IRenderedComponent<MessagesPage> Render(MessageListContext seed)
    {
        _ctx.ComponentFactories.Add(new MessageProviderStubFactory(list: seed));
        return _ctx.Render<MessagesPage>();
    }

    private static MessageListContext Ctx(
        IReadOnlyList<MessagePayload>? messages = null,
        bool isLoading = false,
        string? error = null,
        Func<Task>? onMarkAllRead = null,
        Func<Guid, Task>? onArchive = null,
        Func<Guid, Task>? onDismiss = null) => new()
        {
            Messages = messages ?? [],
            IsLoading = isLoading,
            ErrorMessage = error,
            OnMarkAllRead = onMarkAllRead ?? (() => Task.CompletedTask),
            OnArchive = onArchive ?? (_ => Task.CompletedTask),
            OnDismiss = onDismiss ?? (_ => Task.CompletedTask),
        };

    private static MessagePayload Msg(
        string subject = "Hi", string type = "SystemNotification", string severity = "Normal",
        DateTime? readAt = null, DateTime? archivedAt = null, DateTime? dismissedAt = null) => new()
        {
            Id = Guid.NewGuid(),
            Subject = subject,
            MessageType = type,
            Severity = severity,
            ReadAt = readAt,
            ArchivedAt = archivedAt,
            DismissedAt = dismissedAt,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
        };

    [Fact]
    public void RendersLoadingBranch()
    {
        var cut = Render(Ctx(isLoading: true));
        cut.Markup.ShouldContain("Loading");
    }

    [Fact]
    public void RendersErrorText()
    {
        var cut = Render(Ctx(error: "msg load failed"));
        cut.Markup.ShouldContain("msg load failed");
    }

    [Fact]
    public void RendersEmptyWhenNoMessages()
    {
        var cut = Render(Ctx());
        cut.Markup.ShouldContain("No messages");
    }

    [Fact]
    public void RendersMessageRows()
    {
        var cut = Render(Ctx(messages: [Msg("First"), Msg("Second")]));
        cut.Markup.ShouldContain("First");
        cut.Markup.ShouldContain("Second");
    }

    [Theory]
    [InlineData("Critical", "dot-red")]
    [InlineData("High", "dot-amber")]
    [InlineData("Normal", "dot-glacier")]
    [InlineData("Low", "dot-violet")]
    [InlineData("Weird", "dot-glacier")]
    public void RendersSeverityDotClass(string severity, string dotClass)
    {
        var cut = Render(Ctx(messages: [Msg(severity: severity)]));
        cut.Markup.ShouldContain(dotClass);
    }

    [Fact]
    public void DismissAndArchiveButtonsShownForActiveMessage()
    {
        var cut = Render(Ctx(messages: [Msg("Active")]));
        cut.FindAll("button[title='Dismiss']").Count.ShouldBe(1);
        cut.FindAll("button[title='Archive']").Count.ShouldBe(1);
    }

    [Fact]
    public void DismissHiddenWhenAlreadyDismissed()
    {
        var cut = Render(Ctx(messages: [Msg("Done", dismissedAt: DateTime.UtcNow)]));
        cut.FindAll("button[title='Dismiss']").Count.ShouldBe(0);
    }

    [Fact]
    public void ArchiveHiddenWhenAlreadyArchived()
    {
        var cut = Render(Ctx(messages: [Msg("Done", archivedAt: DateTime.UtcNow)]));
        cut.FindAll("button[title='Archive']").Count.ShouldBe(0);
    }

    [Fact]
    public void UnreadFilterHidesReadMessages()
    {
        var cut = Render(Ctx(messages: [Msg("ZseenMsg", readAt: DateTime.UtcNow), Msg("ZfreshMsg")]));
        cut.FindAll("span.chip").First(c => string.Equals(c.TextContent.Trim(), "Unread", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("ZfreshMsg");
        cut.Markup.ShouldNotContain("ZseenMsg");
    }

    [Fact]
    public void ReadFilterShowsOnlyRead()
    {
        var cut = Render(Ctx(messages: [Msg("ZseenMsg", readAt: DateTime.UtcNow), Msg("ZfreshMsg")]));
        cut.FindAll("span.chip").First(c => string.Equals(c.TextContent.Trim(), "Read", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("ZseenMsg");
        cut.Markup.ShouldNotContain("ZfreshMsg");
    }

    [Fact]
    public void ArchivedFilterShowsOnlyArchived()
    {
        var cut = Render(Ctx(messages: [Msg("Arch", archivedAt: DateTime.UtcNow), Msg("Live")]));
        cut.FindAll("span.chip").First(c => string.Equals(c.TextContent.Trim(), "Archived", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("Arch");
        cut.Markup.ShouldNotContain("Live");
    }

    [Fact]
    public void TypeFilterMatches()
    {
        var cut = Render(Ctx(messages: [Msg("ZsysMsg", type: "SystemNotification"), Msg("ZaccMsg", type: "AccessRequest")]));
        cut.FindAll("select")[0].Change("AccessRequest");
        cut.Markup.ShouldContain("ZaccMsg");
        cut.Markup.ShouldNotContain("ZsysMsg");
    }

    [Fact]
    public void SeverityFilterMatches()
    {
        var cut = Render(Ctx(messages: [Msg("ZcritMsg", severity: "Critical"), Msg("ZnormMsg", severity: "Normal")]));
        cut.FindAll("select")[1].Change("Critical");
        cut.Markup.ShouldContain("ZcritMsg");
        cut.Markup.ShouldNotContain("ZnormMsg");
    }

    [Fact]
    public void FilterWithNoMatchShowsEmpty()
    {
        var cut = Render(Ctx(messages: [Msg("ZnormMsg", severity: "Normal")]));
        cut.FindAll("select")[1].Change("Critical");
        cut.Markup.ShouldContain("No messages");
    }

    [Fact]
    public async Task MarkAllReadInvokesCallback()
    {
        var calls = 0;
        var cut = Render(Ctx(onMarkAllRead: () => { calls++; return Task.CompletedTask; }));
        cut.FindAll("button").First(b => b.TextContent.Contains("Mark All Read", StringComparison.Ordinal)).Click();
        await Task.Yield();
        calls.ShouldBe(1);
    }

    [Fact]
    public async Task DismissInvokesOnDismissWithId()
    {
        var msg = Msg("Active");
        Guid? dismissed = null;
        var cut = Render(Ctx(messages: [msg], onDismiss: g => { dismissed = g; return Task.CompletedTask; }));
        cut.Find("button[title='Dismiss']").Click();
        await Task.Yield();
        dismissed.ShouldBe(msg.Id);
    }

    [Fact]
    public async Task ArchiveInvokesOnArchiveWithId()
    {
        var msg = Msg("Active");
        Guid? archived = null;
        var cut = Render(Ctx(messages: [msg], onArchive: g => { archived = g; return Task.CompletedTask; }));
        cut.Find("button[title='Archive']").Click();
        await Task.Yield();
        archived.ShouldBe(msg.Id);
    }

    [Fact]
    public void RowClickNavigatesToDetail()
    {
        var msg = Msg("Open");
        var cut = Render(Ctx(messages: [msg]));
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        cut.FindAll(".a").First(a => a.GetAttribute("style")?.Contains("cursor:pointer", StringComparison.Ordinal) == true).Click();
        nav.Uri.ShouldEndWith($"/messages/{msg.Id}");
    }

    public void Dispose() => _ctx.Dispose();
}
