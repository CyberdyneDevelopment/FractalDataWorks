using Fdw.Collections.Attributes;
using Fdw.UI.Navigation;

namespace Fdw.Services.Authentication.UI.Pages;

/// <summary>
/// Contributes this package's Authentication pages to <see cref="PageTypes"/>.
/// </summary>
[TypeOption(typeof(PageTypes), "Authentication")]
public sealed class AuthenticationPageType : PageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationPageType"/> class.
    /// </summary>
    public AuthenticationPageType()
        : base(7, "Authentication",
        [
            new Page("ApiKeys", typeof(global::Fdw.UI.Pages.Authentication.Pages.ApiKeysPage), new NavItem("API Keys", "key", NavSections.Security, 80), PageAccess.Authenticated),
        ])
    { }
}
