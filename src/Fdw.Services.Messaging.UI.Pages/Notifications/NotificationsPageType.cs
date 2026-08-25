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
    // Why this type did not exist: both pages below are complete and routable and have been since they
    // were written, but Notifications was the only page folder with no page type. The sidebar is built
    // from declared pages, so a page nobody declares is reachable by typing its URL and by nothing else.
    // Same defect as the two host pages declared in 6374f7b, on the packaged side.
    public NotificationsPageType()
        : base(18, "Notifications",
        [
            new Page("Notifications", typeof(global::Fdw.UI.Pages.Notifications.Pages.NotificationsPage), new NavItem("Notifications", "bell", NavSections.Administration, 60), PageAccess.RequiringPermission("notifications:read")),

            // Why a nav entry and not NavItem.Empty: the rules page reads as a child — its eyebrow is
            // "Administration › Notifications" and it carries a back link — so Empty would be the
            // obvious call. But nothing anywhere links TO it: its only reference in the whole workspace
            // is its own @page directive. Declaring it Empty would leave it exactly as unreachable as
            // not declaring it at all. It gets its own entry until something links to it.
            new Page("NotificationRules", typeof(global::Fdw.UI.Pages.Notifications.Pages.NotificationRulesPage), new NavItem("Notification Rules", "shuffle", NavSections.Administration, 70), PageAccess.RequiringPermission("notifications:read")),
        ])
    { }
}
