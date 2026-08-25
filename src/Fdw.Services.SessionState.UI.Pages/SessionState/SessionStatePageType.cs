using Fdw.Collections.Attributes;
using Fdw.UI.Navigation;

namespace Fdw.Services.SessionState.UI.Pages;

/// <summary>
/// Contributes this package's SessionState pages to <see cref="PageTypes"/>.
/// </summary>
[TypeOption(typeof(PageTypes), "SessionState")]
public sealed class SessionStatePageType : PageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionStatePageType"/> class.
    /// </summary>
    public SessionStatePageType()
        : base(60, "SessionState",
        [
            new Page("SessionState", typeof(global::Fdw.UI.Pages.SessionState.Pages.SessionStatePage), new NavItem("Session State", "database", NavSections.DeveloperTools, 100), PageAccess.Authenticated),
        ])
    { }
}
