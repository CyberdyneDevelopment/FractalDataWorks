using Fdw.Collections.Attributes;
using Fdw.UI.Registration;

namespace Fdw.Agents.UI.Pages;

/// <summary>
/// Contributes this package's Agents pages to <see cref="PageTypes"/>.
/// </summary>
[TypeOption(typeof(PageTypes), "Agents")]
public sealed class AgentsPageType : PageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentsPageType"/> class.
    /// </summary>
    public AgentsPageType()
        : base(1, "Agents",
        [
            new Page("AgentActions", typeof(global::Fdw.Agents.UI.Pages.Pages.AgentActionsPage), new NavItem("Agent Actions", "cpu", NavSections.Observability, 110), PageAccess.RequiringPermission("agent-actions:read")),
            new Page("ReviewAgentAction", typeof(global::Fdw.Agents.UI.Pages.Pages.ReviewAgentActionPage), NavItem.Empty, PageAccess.Authenticated),
        ])
    { }
}
