using Fdw.Collections.Attributes;
using Fdw.UI.Registration;

namespace Fdw.Services.Authorization.UI.Pages;

/// <summary>
/// Contributes this package's Authorization pages to <see cref="PageTypes"/>.
/// </summary>
[TypeOption(typeof(PageTypes), "Authorization")]
public sealed class AuthorizationPageType : PageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationPageType"/> class.
    /// </summary>
    public AuthorizationPageType()
        : base(8, "Authorization",
        [
            // Why: both role pages require settings/role:read — the permission their endpoints now
            // enforce. Roles previously declared no required permission, so the sidebar advertised it
            // to every principal including those whose API calls the role endpoints would refuse.
            new Page("RoleDetail", typeof(Fdw.UI.Pages.Authorization.Pages.RoleDetailPage), NavItem.Empty, "settings/role:read"),
            new Page("Roles", typeof(Fdw.UI.Pages.Authorization.Pages.RolesPage), new NavItem("Roles", "shield", NavSections.Security, 80), "settings/role:read"),
            new Page("Users", typeof(Fdw.UI.Pages.Authorization.Pages.UsersPage), new NavItem("Users", "users", NavSections.Security, 80), "users:read"),
        ])
    { }
}
