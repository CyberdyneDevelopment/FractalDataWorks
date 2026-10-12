using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces;

/// <summary>Whether the caller can view the workspace itself (never the rows of its data sets).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceQuestions), "View")]
public sealed class ViewWorkspaceQuestion : WorkspaceQuestionBase
{
    /// <summary>Initializes a new instance of the <see cref="ViewWorkspaceQuestion"/> class.</summary>
    public ViewWorkspaceQuestion()
        : base("View", "the caller neither owns it, holds a membership that views it, holds workspaces:read, workspaces:query or a permission for this one, nor is it open")
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Querying implies viewing, for the platform permission and the workspace's own alike: rows served
    /// from a workspace the caller could not list would contradict the listing. Every member role views
    /// (Observer views without reading rows).
    /// </remarks>
    public override bool Grants(WorkspaceStanding held)
        => held.IsOwner || held.Holds("workspaces:read") || held.Holds("workspaces:query")
           || held.HoldsForThis(WorkspacePermissionNames.ViewAction) || held.HoldsForThis(WorkspacePermissionNames.QueryAction)
           || held.Roles.Any(role => role.MayView) || held.Visibility.GrantsView;

    /// <inheritdoc />
    /// <remarks>Always a 404: a caller refused a view does not have the workspace (FDW-937).</remarks>
    public override string RefusalCode(bool views) => "WorkspaceNotVisible";
}
