using Fdw.Collections.Attributes;
using Fdw.UI.Registration;

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
            new Page("Audit", typeof(global::Fdw.UI.Pages.Operations.Pages.AuditPage), new NavItem("Audit Log", "file-text", NavSections.Operations, 70), null),
            new Page("Dataflow", typeof(global::Fdw.UI.Pages.Operations.Pages.DataflowPage), new NavItem("Data Flow", "git-branch", NavSections.Operations, 70), null),
            new Page("HealthDashboard", typeof(global::Fdw.UI.Pages.Operations.Pages.HealthDashboardPage), new NavItem("Health", "heart", NavSections.Operations, 70), null),
            new Page("Lineage", typeof(global::Fdw.UI.Pages.Operations.Pages.LineagePage), new NavItem("Lineage", "share-2", NavSections.Observability, 110), null),
            // Why: the sidebar places Promotions under "Quality" even though Operations owns the page.
            // A nav entry's section is a placement choice, independent of which package owns the page —
            // which is what lets this stay where users expect it. Previously QualityPageType declared this
            // entry while Operations held the component, so the two could disagree with nothing noticing.
            new Page("Promotions", typeof(global::Fdw.UI.Pages.Operations.Pages.Promotions.PromotionsIndexPage), new NavItem("Promotions", "arrow-up-circle", NavSections.Quality, 50), null),
            new Page("PromotionReview", typeof(global::Fdw.UI.Pages.Operations.Pages.Promotions.PromotionReviewPage), NavItem.Empty, null),
        ])
    { }
}
