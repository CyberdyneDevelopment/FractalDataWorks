using Fdw.Collections.Attributes;
using Fdw.UI.Registration;

namespace Fdw.Services.SecretManagers.UI.Pages;

/// <summary>
/// Contributes this package's SecretManagers pages to <see cref="PageTypes"/>.
/// </summary>
[TypeOption(typeof(PageTypes), "SecretManagers")]
public sealed class SecretManagersPageType : PageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagersPageType"/> class.
    /// </summary>
    public SecretManagersPageType()
        : base(15, "SecretManagers",
        [
            new Page("SecretManagers", typeof(global::Fdw.UI.Pages.SecretManagers.Pages.SecretManagersPage), new NavItem("Secret Managers", "lock", NavSections.Configuration, 90), PageAccess.RequiringPermission("secretmanagers:read")),
        ])
    { }
}
