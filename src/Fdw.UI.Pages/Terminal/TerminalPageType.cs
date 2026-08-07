using Fdw.Collections.Attributes;
using Fdw.UI.Registration;

namespace Fdw.Services.Terminal.UI.Pages;

/// <summary>
/// Contributes this package's Terminal pages to <see cref="PageTypes"/>.
/// </summary>
[TypeOption(typeof(PageTypes), "Terminal")]
public sealed class TerminalPageType : PageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalPageType"/> class.
    /// </summary>
    public TerminalPageType()
        : base(16, "Terminal",
        [
            new Page("Terminal", typeof(global::Fdw.UI.Pages.Terminal.Pages.TerminalPage), new NavItem("Terminal", "terminal", NavSections.Observability, 110), PageAccess.Authenticated),
        ])
    { }
}
