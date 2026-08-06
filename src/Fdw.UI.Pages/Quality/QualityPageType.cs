using Fdw.Collections.Attributes;
using Fdw.UI.Registration;

namespace Fdw.Services.Quality.UI.Pages;

/// <summary>
/// Contributes this package's Quality pages to <see cref="PageTypes"/>.
/// </summary>
[TypeOption(typeof(PageTypes), "Quality")]
public sealed class QualityPageType : PageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QualityPageType"/> class.
    /// </summary>
    public QualityPageType()
        : base(13, "Quality",
        [
            new Page("Dashboard", typeof(global::Fdw.UI.Pages.Quality.Pages.Quality.QualityDashboardPage), new NavItem("Quality Reports", "bar-chart", NavSections.Quality, 50), null),
            new Page("Rules", typeof(global::Fdw.UI.Pages.Quality.Pages.Quality.QualityRulesPage), new NavItem("Quality Rules", "check-circle", NavSections.Quality, 50), "quality/rules:read"),
        ])
    { }
}
