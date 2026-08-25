using Fdw.Collections.Attributes;
using Fdw.UI.Navigation;

namespace Fdw.Services.Messaging.UI.Pages;

/// <summary>
/// Contributes this package's Messaging pages to <see cref="PageTypes"/>.
/// </summary>
[TypeOption(typeof(PageTypes), "Messaging")]
public sealed class MessagingPageType : PageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingPageType"/> class.
    /// </summary>
    public MessagingPageType()
        : base(11, "Messaging",
        [
            new Page("AccessRequests", typeof(global::Fdw.UI.Pages.Messaging.Pages.AccessRequestsPage), new NavItem("Access Requests", "user-check", NavSections.Security, 80), PageAccess.RequiringPermission("access-requests:read")),
            new Page("MessageDetail", typeof(global::Fdw.UI.Pages.Messaging.Pages.MessageDetailPage), NavItem.Empty, PageAccess.Authenticated),
            new Page("Messages", typeof(global::Fdw.UI.Pages.Messaging.Pages.MessagesPage), new NavItem("Messages", "message-square", NavSections.Observability, 110), PageAccess.RequiringPermission("messages:read")),
            new Page("NewAccessRequest", typeof(global::Fdw.UI.Pages.Messaging.Pages.NewAccessRequestPage), NavItem.Empty, PageAccess.Authenticated),
            new Page("NotificationSettings", typeof(global::Fdw.UI.Pages.Messaging.Pages.NotificationSettingsPage), new NavItem("Notification Settings", "bell", NavSections.Configuration, 90), PageAccess.RequiringPermission("notifications:read")),
        ])
    { }
}
