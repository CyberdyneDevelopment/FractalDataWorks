using Fdw.Collections.Attributes;
using Fdw.UI.Registration;

namespace Fdw.Calculations.UI.Pages;

/// <summary>
/// Contributes this package's Calculations pages to <see cref="PageTypes"/>.
/// </summary>
[TypeOption(typeof(PageTypes), "Calculations")]
public sealed class CalculationsPageType : PageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationsPageType"/> class.
    /// </summary>
    public CalculationsPageType()
        : base(2, "Calculations",
        [
            new Page("CalculatedDesigner", typeof(global::Fdw.UI.Pages.Calculations.Pages.CalculatedDesignerPage), new NavItem("DataSet Designer", "calculator", NavSections.Transformations, 20), PageAccess.Authenticated),
            new Page("Calculations", typeof(global::Fdw.UI.Pages.Calculations.Pages.CalculationsPage), new NavItem("Calculations", "calculator", NavSections.Transformations, 20), PageAccess.RequiringPermission("calculations:read")),
            new Page("CalculationsCreate", typeof(global::Fdw.UI.Pages.Calculations.Pages.CalculationsCreatePage), NavItem.Empty, PageAccess.Authenticated),
            new Page("CalculationsEdit", typeof(global::Fdw.UI.Pages.Calculations.Pages.CalculationsEditPage), NavItem.Empty, PageAccess.Authenticated),
        ])
    { }
}
