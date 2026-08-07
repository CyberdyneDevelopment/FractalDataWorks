using Fdw.Collections.Attributes;
using Fdw.UI.Registration;

namespace Fdw.Services.Pipelines.UI.Pages;

/// <summary>
/// Contributes this package's Pipelines pages to <see cref="PageTypes"/>.
/// </summary>
[TypeOption(typeof(PageTypes), "Pipelines")]
public sealed class PipelinesPageType : PageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelinesPageType"/> class.
    /// </summary>
    public PipelinesPageType()
        : base(12, "Pipelines",
        [
            new Page("Builder", typeof(global::Fdw.UI.Pages.Pipelines.Pages.Pipelines.PipelineBuilderPage), NavItem.Empty, PageAccess.Authenticated),
            new Page("CreatePipeline", typeof(global::Fdw.UI.Pages.Pipelines.Pages.Pipelines.CreatePipelinePage), NavItem.Empty, PageAccess.Authenticated),
            new Page("ExecutionDetail", typeof(global::Fdw.UI.Pages.Pipelines.Pages.Pipelines.ExecutionDetailPage), NavItem.Empty, PageAccess.Authenticated),
            new Page("Index", typeof(global::Fdw.UI.Pages.Pipelines.Pages.Pipelines.PipelinesIndexPage), new NavItem("Pipelines", "activity", NavSections.Pipelines, 30), PageAccess.RequiringPermission("pipelines:read")),
        ])
    { }
}
