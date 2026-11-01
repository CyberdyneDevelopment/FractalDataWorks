using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Workspaces.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Decides whether the calling user may view, change, or query a particular workspace, or find it to ask to join.
/// </summary>
/// <remarks>
/// <c>workspaces:write</c> answers "may this caller have workspaces at all". It does not answer
/// "which ones", and until this existed nothing did: the row-level policy filters on tenant and
/// visibility group, the endpoints never read <c>OwnerUserId</c> or <c>WorkspaceMember</c>, and the
/// four member roles were a vocabulary no authorization path consulted. Anyone holding the
/// permission could edit or delete every workspace in the tenant.
/// <para>
/// One decision, in one place, because the next workspace-scoped write — a note, a resource, a
/// relationship — asks the same question, and a rule re-derived per endpoint is a rule that will
/// eventually differ per endpoint.
/// </para>
/// <para>
/// Every answer is additive (FDW-936): the platform permission OR standing in the workspace
/// (ownership, a membership whose role answers the question, and for seeing and viewing its
/// visibility). A refusal of seeing, viewing or changing answers 404 when the caller cannot see the
/// workspace at all and 403 once they can, so a refusal never confirms a name the caller could not
/// otherwise find.
/// </para>
/// </remarks>
public interface IWorkspaceAccessPolicy
{
    /// <summary>Whether the calling user may find <paramref name="workspace"/> to ask to join it.</summary>
    /// <param name="workspace">The workspace, with its members loaded.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success when the caller may ask to join it; a 404-category failure when they may not.</returns>
    /// <remarks>
    /// Anyone who may view it, any in-effect membership, or a visibility that lists it (Discoverable,
    /// Open). Only the membership request asks this. Listings ask <see cref="MayView"/>: a workspace
    /// is listed for exactly the callers who have it (FDW-937).
    /// </remarks>
    Task<IGenericResult> MaySee(IWorkspaceImplementationConfiguration workspace, CancellationToken cancellationToken = default);

    /// <summary>Whether the calling user may view <paramref name="workspace"/>: its page, map, readiness, members, notes and data views.</summary>
    /// <param name="workspace">The workspace, with its members loaded.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success when the caller may view it; a failure carrying the reason when they may not.</returns>
    /// <remarks>
    /// Owner, a membership whose role views (every member role does; Observer views without reading rows),
    /// <c>workspaces:read</c> or <c>workspaces:query</c>, <c>workspaces/{id}:view</c> or <c>:query</c>,
    /// or a visibility that grants view
    /// (Open). Viewing never includes the rows of the data sets attached to it -- that is <see cref="MayRead"/>.
    /// </remarks>
    Task<IGenericResult> MayView(IWorkspaceImplementationConfiguration workspace, CancellationToken cancellationToken = default);

    /// <summary>Whether the calling user may change <paramref name="workspace"/>.</summary>
    /// <param name="workspace">The workspace, with its members loaded.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Success when the caller may write it; a failure carrying the reason when they may not.
    /// A result rather than a bool, so a refusal reaches the caller as the refusal it is instead of
    /// as a value somebody has to remember to turn back into one.
    /// </returns>
    Task<IGenericResult> MayWrite(IWorkspaceImplementationConfiguration workspace, CancellationToken cancellationToken = default);

    /// <summary>Whether the calling user may query the data sets attached to <paramref name="workspace"/>.</summary>
    /// <param name="workspace">The workspace, with its members loaded.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success when the caller may read it; a failure carrying the reason when they may not.</returns>
    /// <remarks>
    /// The owner, a member whose membership is in effect and whose role reads, or a holder of
    /// <c>workspaces:query</c> (FDW-936). Visibility never grants rows. A third party's API key rides
    /// on membership (FDW-876): a principal holding no platform permission reaches exactly the
    /// workspaces it is a member of, Open or not.
    /// </remarks>
    Task<IGenericResult> MayRead(IWorkspaceImplementationConfiguration workspace, CancellationToken cancellationToken = default);

    /// <summary>Whether the calling user may decide who is in <paramref name="workspace"/>: list, approve and deny requests to join.</summary>
    /// <param name="workspace">The workspace, with its members loaded.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success when the caller manages its members; a failure carrying the reason when they may not.</returns>
    /// <remarks>
    /// The owner, a member whose role manages members (Owner, Steward), or a holder of
    /// <c>workspaces:members</c> (FDW-1071). <c>workspaces:write</c> does not answer it: changing a
    /// workspace is not deciding who belongs to it.
    /// </remarks>
    Task<IGenericResult> MayManageMembers(IWorkspaceImplementationConfiguration workspace, CancellationToken cancellationToken = default);

    /// <summary>Whether the calling user may grant, change or remove a membership holding <paramref name="role"/>.</summary>
    /// <param name="workspace">The workspace, with its members loaded.</param>
    /// <param name="role">The role being granted, or the role the member holds now.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success when the caller manages members and stands at or above the role; a failure carrying the reason otherwise.</returns>
    /// <remarks>
    /// <see cref="MayManageMembers"/>, then rank: a member grants and changes only roles at or below their
    /// own, so a Steward cannot make an Owner or remove one (FDW-1071). The owner and holders of
    /// <c>workspaces:members</c> handle every role. An unknown role (the NotFound option) has rank 0.
    /// </remarks>
    Task<IGenericResult> MayManageRole(
        IWorkspaceImplementationConfiguration workspace, IWorkspaceMemberRole role, CancellationToken cancellationToken = default);

    /// <summary>Whether the calling user may attach the data set <paramref name="dataSetId"/> to <paramref name="workspace"/>.</summary>
    /// <param name="workspace">The workspace, with its members loaded.</param>
    /// <param name="dataSetId">The data set to attach.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success when the caller may attach it; a failure carrying the reason when they may not.</returns>
    /// <remarks>
    /// The one decision for granting a data set through a workspace (FDW-937). A workspace serves its
    /// data sets past row-level security, so attaching one is granting its rows to every member. It
    /// takes all three: the caller may change the workspace (<see cref="MayWrite"/>); the data set
    /// resolves as the caller, so row-level security lets them see it (a 404 when it does not); and
    /// the caller holds a grant for it -- <c>datasets:grant</c> for their tenant,
    /// <c>datasets/{id}:grant</c> for the one set, <c>datasets:*</c> or <c>*:*</c>.
    /// </remarks>
    Task<IGenericResult> MayAttachDataSet(
        IWorkspaceImplementationConfiguration workspace, Guid dataSetId, CancellationToken cancellationToken = default);
}
