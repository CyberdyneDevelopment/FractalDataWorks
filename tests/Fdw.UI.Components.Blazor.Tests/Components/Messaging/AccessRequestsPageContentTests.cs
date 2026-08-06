using Bunit;
using Bunit.ComponentFactories;
using Fdw.Services.Messaging.Clients.Models;
using Fdw.Services.Messaging.Components.Messaging;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using AccessRequestsPage = Fdw.Services.Messaging.UI.Pages.Pages.AccessRequests;

namespace Fdw.UI.Components.Blazor.Tests.Components.Messaging;

/// <summary>
/// Component tests for the FDW <c>AccessRequests</c> page (Messaging.UI.Pages). Relocated from
/// reference-ui's Messaging/AccessRequestsPageTests, which asserted these behaviours through the
/// hosted page; here they run directly against the page component with a seeded
/// <see cref="AccessRequestListContext"/> swapped in for the live <c>MessageProvider</c>. Status
/// badge / filter assertions target the page's CURRENT markup (badge classes b-warn/b-run/b-fail/
/// b-idle; chip filter spans).
/// </summary>
[Trait("Category", "Ui")]
public sealed class AccessRequestsPageContentTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private IRenderedComponent<AccessRequestsPage> Render(AccessRequestListContext seed)
    {
        _ctx.ComponentFactories.Add(new MessageProviderStubFactory(access: seed));
        return _ctx.Render<AccessRequestsPage>();
    }

    private static AccessRequestListContext Ctx(
        IReadOnlyList<AccessRequestPayload>? requests = null,
        bool isLoading = false,
        string? error = null,
        Func<Guid, string?, Task>? onApprove = null,
        Func<Guid, string?, Task>? onDeny = null) => new()
        {
            AccessRequests = requests ?? [],
            IsLoading = isLoading,
            ErrorMessage = error,
            OnApprove = onApprove ?? ((_, _) => Task.CompletedTask),
            OnDeny = onDeny ?? ((_, _) => Task.CompletedTask),
        };

    private static AccessRequestPayload Req(string status = "Pending", string? notes = null) => new()
    {
        Id = Guid.NewGuid(),
        RequestedResource = "Connection:Prod",
        RequestedPermission = "Read",
        Justification = "need it",
        Status = status,
        ReviewNotes = notes,
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public void RendersLoadingBranch()
    {
        var cut = Render(Ctx(isLoading: true));
        cut.Markup.ShouldContain("Loading");
    }

    [Fact]
    public void RendersError()
    {
        var cut = Render(Ctx(error: "ar load failed"));
        cut.Markup.ShouldContain("ar load failed");
    }

    [Fact]
    public void RendersEmpty()
    {
        var cut = Render(Ctx());
        cut.Markup.ShouldContain("No access requests");
    }

    [Fact]
    public void RendersRows()
    {
        var cut = Render(Ctx(requests: [Req(), Req()]));
        cut.FindAll("tbody tr").Count.ShouldBe(2);
        cut.Markup.ShouldContain("Connection:Prod");
    }

    [Theory]
    [InlineData("Pending", "b-warn")]
    [InlineData("Approved", "b-run")]
    [InlineData("Denied", "b-fail")]
    [InlineData("Other", "b-idle")]
    public void RendersStatusBadge(string status, string fragment)
    {
        var cut = Render(Ctx(requests: [Req(status)]));
        cut.Markup.ShouldContain(fragment);
    }

    [Fact]
    public void ApproveDenyShownOnlyForPending()
    {
        var cut = Render(Ctx(requests: [Req("Approved")]));
        cut.FindAll("button[title='Approve']").Count.ShouldBe(0);
        cut.FindAll("button[title='Deny']").Count.ShouldBe(0);
    }

    [Fact]
    public void ReviewNotesIconShownForNonPendingWithNotes()
    {
        var cut = Render(Ctx(requests: [Req("Denied", notes: "not allowed")]));
        cut.Markup.ShouldContain("not allowed");
    }

    [Fact]
    public async Task ApproveInvokesOnApprove()
    {
        var req = Req("Pending");
        Guid? approved = null;
        var cut = Render(Ctx(requests: [req], onApprove: (g, _) => { approved = g; return Task.CompletedTask; }));
        cut.Find("button[title='Approve']").Click();
        await Task.Yield();
        approved.ShouldBe(req.Id);
    }

    [Fact]
    public void FilterPendingHidesOthers()
    {
        var cut = Render(Ctx(requests: [Req("Pending"), Req("Approved")]));
        cut.FindAll("span.chip").First(c => string.Equals(c.TextContent.Trim(), "Approved", StringComparison.Ordinal)).Click();
        cut.FindAll("button[title='Approve']").Count.ShouldBe(0);
    }

    [Fact]
    public void DenyOpensDialog()
    {
        var cut = Render(Ctx(requests: [Req("Pending")]));
        cut.Find("button[title='Deny']").Click();
        cut.Markup.ShouldContain("Deny Access Request");
    }

    [Fact]
    public void DenyDialogCancelCloses()
    {
        var cut = Render(Ctx(requests: [Req("Pending")]));
        cut.Find("button[title='Deny']").Click();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Cancel", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldNotContain("Deny Access Request");
    }

    [Fact]
    public async Task DenyDialogConfirmInvokesOnDenyWithNotes()
    {
        var req = Req("Pending");
        (Guid Id, string? Notes)? denied = null;
        var cut = Render(Ctx(requests: [req], onDeny: (g, n) => { denied = (g, n); return Task.CompletedTask; }));
        cut.Find("button[title='Deny']").Click();
        cut.Find("textarea").Change("policy violation");
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Deny", StringComparison.Ordinal)).Click();
        await Task.Yield();
        denied.ShouldNotBeNull();
        denied!.Value.Id.ShouldBe(req.Id);
        denied.Value.Notes.ShouldBe("policy violation");
    }

    public void Dispose() => _ctx.Dispose();
}
