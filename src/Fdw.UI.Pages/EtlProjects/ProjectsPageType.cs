using Fdw.Collections.Attributes;
using Fdw.UI.Registration;

namespace Fdw.Services.Etl.Projects.UI.Pages;

/// <summary>
/// Contributes this package's Projects pages to <see cref="PageTypes"/>.
/// </summary>
[TypeOption(typeof(PageTypes), "Projects")]
public sealed class ProjectsPageType : PageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectsPageType"/> class.
    /// </summary>
    public ProjectsPageType()
        : base(17, "Projects",
        [
            new Page("NodeList", typeof(global::Fdw.UI.Pages.EtlProjects.Pages.Orchestration.NodeListPage), new NavItem("Orchestration", "git-branch", NavSections.Pipelines, 30), PageAccess.Authenticated),
            new Page("NodeTreeEditor", typeof(global::Fdw.UI.Pages.EtlProjects.Pages.Orchestration.NodeTreeEditorPage), NavItem.Empty, PageAccess.Authenticated),
            new Page("ProjectEdit", typeof(global::Fdw.UI.Pages.EtlProjects.Pages.Projects.ProjectEditPage), NavItem.Empty, PageAccess.Authenticated),
            new Page("ProjectExecution", typeof(global::Fdw.UI.Pages.EtlProjects.Pages.Projects.ProjectExecutionPage), NavItem.Empty, PageAccess.Authenticated),
            new Page("ProjectIndex", typeof(global::Fdw.UI.Pages.EtlProjects.Pages.Projects.ProjectIndexPage), new NavItem("Projects", "folder", NavSections.Pipelines, 30), PageAccess.Authenticated),
            new Page("ProjectList", typeof(global::Fdw.UI.Pages.EtlProjects.Pages.Projects.ProjectListPage), NavItem.Empty, PageAccess.Authenticated),
        ])
    { }
}
