using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Removes one member, one declared relationship or one attached resource from a workspace by retiring its row by Id (FDW-7024, FDW-7093).
/// </summary>
/// <remarks>
/// A workspace's members, relationships and resources are upsert-only child lists whose rows hang from the workspace's durable Id, so saving the workspace
/// without one keeps it current. This retires the row itself and never saves the workspace. It decides nothing about who may: the endpoint asks
/// <see cref="IWorkspaceAccessPolicy"/> first, as before.
/// </remarks>
public interface IWorkspaceChildRetirement
{
    /// <summary>Removes a member: retires its row and takes it out of the workspace as read.</summary>
    /// <param name="workspace">The workspace, with its members loaded.</param>
    /// <param name="member">The member, as found among the workspace's members.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success; a coded failure when the row cannot be retired, the member then still in the workspace.</returns>
    Task<IGenericResult> RemoveMember(IWorkspaceImplementationConfiguration workspace, WorkspaceMemberConfiguration member, CancellationToken cancellationToken = default);

    /// <summary>Detaches a declared relationship: retires its row and takes it out of the workspace as read.</summary>
    /// <param name="workspace">The workspace, with its relationships loaded.</param>
    /// <param name="relationship">The relationship, as found among the workspace's relationships.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success; a coded failure when the row cannot be retired, the relationship then still in the workspace.</returns>
    Task<IGenericResult> DetachRelationship(
        IWorkspaceImplementationConfiguration workspace, WorkspaceRelationshipConfiguration relationship, CancellationToken cancellationToken = default);

    /// <summary>Detaches a resource: retires its row and takes it out of the workspace as read (FDW-7024).</summary>
    /// <param name="workspace">The workspace, with its resources loaded.</param>
    /// <param name="resource">The resource, as found among the workspace's resources.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success; a coded failure when the row cannot be retired, the resource then still in the workspace.</returns>
    Task<IGenericResult> DetachResource(
        IWorkspaceImplementationConfiguration workspace, WorkspaceResourceConfiguration resource, CancellationToken cancellationToken = default);
}
