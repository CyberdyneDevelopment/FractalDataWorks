using Fdw.Collections.Attributes;
using Fdw.UI.Navigation;

namespace Fdw.Services.Scheduling.UI.Pages;

/// <summary>
/// Contributes this package's Scheduling pages to <see cref="PageTypes"/>.
/// </summary>
[TypeOption(typeof(PageTypes), "Scheduling")]
public sealed class SchedulingPageType : PageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulingPageType"/> class.
    /// </summary>
    public SchedulingPageType()
        : base(14, "Scheduling",
        [
            new Page("Index", typeof(global::Fdw.UI.Pages.Scheduling.Pages.Schedules.SchedulesIndexPage), new NavItem("Schedules", "clock", NavSections.Scheduling, 40), PageAccess.RequiringPermission("schedules:read")),
            new Page("New", typeof(global::Fdw.UI.Pages.Scheduling.Pages.Schedules.NewSchedulePage), NavItem.Empty, PageAccess.Authenticated),
        ])
    { }
}
