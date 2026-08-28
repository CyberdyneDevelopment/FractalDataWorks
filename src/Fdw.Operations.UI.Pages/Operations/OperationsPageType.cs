using Fdw.Collections.Attributes;
using Fdw.UI.Navigation;

namespace Fdw.Operations.UI.Pages;

/// <summary>
/// Contributes this package's Operations pages to <see cref="PageTypes"/>.
/// </summary>
[TypeOption(typeof(PageTypes), "Operations")]
public sealed class OperationsPageType : PageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationsPageType"/> class.
    /// </summary>
    public OperationsPageType()
        : base(5, "Operations",
        [
            new Page("Audit", typeof(global::Fdw.UI.Pages.Operations.Pages.AuditPage), new NavItem("Audit Log", "file-text", NavSections.Operations, 70), PageAccess.Authenticated),
            new Page("Dataflow", typeof(global::Fdw.UI.Pages.Operations.Pages.DataflowPage), new NavItem("Data Flow", "git-branch", NavSections.Operations, 70), PageAccess.Authenticated),
            new Page("HealthDashboard", typeof(global::Fdw.UI.Pages.Operations.Pages.HealthDashboardPage), new NavItem("Health", "heart", NavSections.Operations, 70), PageAccess.Authenticated),
            new Page("Lineage", typeof(global::Fdw.UI.Pages.Operations.Pages.LineagePage), new NavItem("Lineage", "share-2", NavSections.Observability, 110), PageAccess.Authenticated),
            new Page("Promotions", typeof(global::Fdw.UI.Pages.Operations.Pages.Promotions.PromotionsIndexPage), new NavItem("Promotions", "arrow-up-circle", NavSections.Quality, 50), PageAccess.Authenticated),
            new Page("PromotionReview", typeof(global::Fdw.UI.Pages.Operations.Pages.Promotions.PromotionReviewPage), NavItem.Empty, PageAccess.Authenticated),
        ])
    { }
}
