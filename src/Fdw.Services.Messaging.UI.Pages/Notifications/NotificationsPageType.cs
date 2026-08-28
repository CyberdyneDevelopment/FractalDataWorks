using Fdw.Collections.Attributes;
using Fdw.UI.Navigation;

namespace Fdw.Services.Notifications.UI.Pages;

/// <summary>
/// Contributes this package's Notifications pages to <see cref="PageTypes"/>.
/// </summary>
[TypeOption(typeof(PageTypes), "Notifications")]
public sealed class NotificationsPageType : PageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationsPageType"/> class.
    /// </summary>
    public NotificationsPageType()
        : base(18, "Notifications",
        [
            new Page("Notifications", typeof(global::Fdw.UI.Pages.Notifications.Pages.NotificationsPage), new NavItem("Notifications", "bell", NavSections.Administration, 60), PageAccess.RequiringPermission("notifications:read")),

            new Page("NotificationRules", typeof(global::Fdw.UI.Pages.Notifications.Pages.NotificationRulesPage), new NavItem("Notification Rules", "shuffle", NavSections.Administration, 70), PageAccess.RequiringPermission("notifications:read")),
        ])
    { }
}
