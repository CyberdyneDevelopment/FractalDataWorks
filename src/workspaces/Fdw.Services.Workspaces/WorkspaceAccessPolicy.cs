using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Fdw.Services.Workspaces.Abstractions;
using Fdw.Services.Workspaces.Logging;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Decides who may view, change and query a workspace, who may find one to ask to join, and who may
/// grant a data set through one.
/// </summary>
/// <remarks>
/// <para>
/// Access is additive (FDW-936): the platform permission OR standing in the workspace, never both
/// required. Standing is owning it, or a membership that is in effect and whose role answers the
/// question; for viewing, the workspace's visibility can grant it too. Admins and operators
/// therefore reach every workspace through their permission, and a member reaches theirs whether or
/// not their role carries the permission.
/// </para>
/// <para>
/// Viewing is having it (FDW-937): a workspace is listed for exactly the callers who may view it, and
/// every refusal to a caller who may not view it is a 404, so no answer confirms a workspace the
/// caller does not have. Finding one to ask to join (<see cref="MaySee"/>) is the only wider question,
/// and only the membership request asks it.
/// </para>
/// <para>
/// Rows are the exception visibility never grants. Querying the data sets attached to a workspace
/// needs ownership, a membership whose role reads, or <c>workspaces:query</c> -- so a principal
/// confined by membership to one workspace cannot read another just because it is Open (FDW-876).
/// </para>
/// <para>
/// Every answer is read off an option -- <see cref="WorkspaceQuestionBase"/> carries each question's rule,
/// refusal code and reason, <see cref="WorkspaceMemberStateBase"/> says whether someone is
/// here, <see cref="WorkspaceMemberRoleBase"/> what they may do, <see cref="WorkspaceVisibilityBase"/>
/// what a non-member gets -- so a state, role or visibility added later arrives with its own answer.
/// Fail-closed throughout: an unresolvable caller or a name this deployment does not know is a refusal
/// with the reason, never an assumption that it was probably fine. An unreadable role list is not a
/// refusal but <c>WorkspaceAccessUndetermined</c>: answering an outage as "no access" would hide it.
/// </para>
/// <para>
/// Role memberships match against the roles baked into the caller's token, and platform permissions
/// against its baked permissions -- the same sets every other check reads, resolved at sign-in, so a
/// revocation takes effect when the token is renewed.
/// </para>
/// </remarks>
public sealed class WorkspaceAccessPolicy : IWorkspaceAccessPolicy
{
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly IRoleConfigurationProvider _roles;
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceAccessPolicy"/> class.</summary>
    /// <param name="authContext">Supplies the calling user.</param>
    /// <param name="roles">Resolves the caller's role names to the ids a membership records.</param>
    /// <param name="dataSets">Resolves a data set as the caller, for deciding whether they may grant it.</param>
    /// <param name="logger">The logger.</param>
    public WorkspaceAccessPolicy(
        IAuthenticationContextAccessor authContext,
        IRoleConfigurationProvider roles,
        IDataSetConfigurationProvider dataSets,
        ILogger<WorkspaceAccessPolicy>? logger = null)
    {
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _dataSets = dataSets ?? throw new ArgumentNullException(nameof(dataSets));
        _logger = logger ?? NullLogger<WorkspaceAccessPolicy>.Instance;
    }

    /// <inheritdoc />
    public Task<IGenericResult> MaySee(
        IWorkspaceImplementationConfiguration workspace, CancellationToken cancellationToken = default)
    {
        if (workspace is null) throw new ArgumentNullException(nameof(workspace));
        return Answer(workspace, WorkspaceQuestions.See, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult> MayView(
        IWorkspaceImplementationConfiguration workspace, CancellationToken cancellationToken = default)
    {
        if (workspace is null) throw new ArgumentNullException(nameof(workspace));
        return Answer(workspace, WorkspaceQuestions.View, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult> MayWrite(
        IWorkspaceImplementationConfiguration workspace, CancellationToken cancellationToken = default)
    {
        if (workspace is null) throw new ArgumentNullException(nameof(workspace));
        return Answer(workspace, WorkspaceQuestions.Write, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult> MayRead(
        IWorkspaceImplementationConfiguration workspace, CancellationToken cancellationToken = default)
    {
        if (workspace is null) throw new ArgumentNullException(nameof(workspace));
        return Answer(workspace, WorkspaceQuestions.Read, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult> MayManageMembers(
        IWorkspaceImplementationConfiguration workspace, CancellationToken cancellationToken = default)
    {
        if (workspace is null) throw new ArgumentNullException(nameof(workspace));
        return Answer(workspace, WorkspaceQuestions.ManageMembers, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> MayManageRole(
        IWorkspaceImplementationConfiguration workspace, IWorkspaceMemberRole role, CancellationToken cancellationToken = default)
    {
        if (workspace is null) throw new ArgumentNullException(nameof(workspace));
        if (role is null) throw new ArgumentNullException(nameof(role));

        var (held, unresolved, undetermined) = await StandingOf(workspace, cancellationToken).ConfigureAwait(false);
        var manages = Decide(workspace, WorkspaceQuestions.ManageMembers, held, unresolved, undetermined);
        if (manages.IsFailure) return manages;

        return held!.Outranks(role)
            ? GenericResult.Success()
            : Deny("WorkspaceMemberRankExceeded", workspace.Name,
                $"the role '{role.Name}' ranks above every role through which the caller manages members");
    }

    /// <inheritdoc />
    public async Task<IGenericResult> MayAttachDataSet(
        IWorkspaceImplementationConfiguration workspace, Guid dataSetId, CancellationToken cancellationToken = default)
    {
        if (workspace is null) throw new ArgumentNullException(nameof(workspace));
        var dataSetText = dataSetId.ToString("D", CultureInfo.InvariantCulture);
        DataSetGrantLog.DecidingGrant(_logger, workspace.Name, dataSetText);

        var writes = await Answer(workspace, WorkspaceQuestions.Write, cancellationToken).ConfigureAwait(false);
        if (writes.IsFailure) return writes;

        var dataSet = await _dataSets.Get(dataSetId, cancellationToken).ConfigureAwait(false);
        if (dataSet.IsFailure) return dataSet;
        if (dataSet.Value is null)
        {
            return GenericResult.Failure(
                WorkspacesResultCodes.ByName("DataSetNotVisible"), _logger,
                ResultDetails.Create("dataSet", dataSetText));
        }

        if (!HoldsGrant(_authContext.Current, dataSetId))
        {
            return GenericResult.Failure(
                WorkspacesResultCodes.ByName("DataSetGrantNotPermitted"), _logger,
                ResultDetails.Create("dataSet", dataSet.Value.Name, "name", workspace.Name,
                    "reason", "the caller holds neither datasets:grant nor datasets/{id}:grant for it"));
        }

        DataSetGrantLog.GrantHeld(_logger, workspace.Name, dataSetText);
        return GenericResult.Success();
    }

    // A grant for the data set: the tenant-wide permission, the one for this set, or a wildcard, as PermissionCoverage decides. The
    // token's permissions are its tenant's, so datasets:grant is a grant over that tenant's data sets.
    private static bool HoldsGrant(IAuthenticationContext? caller, Guid dataSetId)
    {
        if (caller is null)
            return false;

        var forThisSet = string.Create(CultureInfo.InvariantCulture, $"datasets/{dataSetId:D}:grant");
        return PermissionCoverage.Covers(caller.Permissions, forThisSet);
    }

    private async Task<IGenericResult> Answer(
        IWorkspaceImplementationConfiguration workspace, IWorkspaceQuestion question, CancellationToken cancellationToken)
    {
        var (held, unresolved, undetermined) = await StandingOf(workspace, cancellationToken).ConfigureAwait(false);
        return Decide(workspace, question, held, unresolved, undetermined);
    }

    // One question answered from a standing already worked out, so a caller asking a second thing of the
    // same standing (MayManageRole's rank) does not read the roles twice.
    private IGenericResult Decide(
        IWorkspaceImplementationConfiguration workspace, IWorkspaceQuestion question, WorkspaceStanding? held, string unresolved, bool undetermined)
    {
        if (undetermined)
            return Deny("WorkspaceAccessUndetermined", workspace.Name, unresolved);
        if (held is null)
            return Deny(question.RefusalCode(views: false), workspace.Name, unresolved);

        if (question.Grants(held))
            return GenericResult.Success();

        // Whether the caller has the workspace at all is what separates a 404 from a 403: one they may
        // view is no secret, so refusing a change to it says so.
        return Deny(question.RefusalCode(views: WorkspaceQuestions.View.Grants(held)), workspace.Name, question.Reason);
    }

    // The caller's standing, or -- when it cannot be worked out -- why not. The reason travels to the
    // refusal, which carries it under the refusal's own code; there is no separate failure to report.
    private async Task<(WorkspaceStanding? Held, string Unresolved, bool Undetermined)> StandingOf(
        IWorkspaceImplementationConfiguration workspace, CancellationToken cancellationToken)
    {
        // Who the caller is decides a refusal; what could not be read decides nothing. A missing
        // context or a non-Guid principal (a service identity) is simply not a member. An unreadable
        // role store is an outage, and answering it as "no access" would hide it (FDW-937).
        if (_authContext.Current is not { } caller)
            return (null, "no authentication context", false);
        if (!Guid.TryParse(caller.UserId, out var callerUserId))
            return (null, "the caller's UserId is not a Guid", false);

        var callerRoleIds = await ResolveRoleIds(caller, cancellationToken).ConfigureAwait(false);
        if (callerRoleIds.IsFailure || callerRoleIds.Value is null)
            return (null, "the caller's roles could not be read", true);

        return (new WorkspaceStanding(
            workspace.Id,
            workspace.OwnerUserId == callerUserId,
            [.. caller.Permissions],
            [.. workspace.Members
                .Where(m => InEffectFor(m, callerUserId, callerRoleIds.Value))
                .Select(m => (IWorkspaceMemberRole)WorkspaceMemberRoles.ByName(m.MemberRole))],
            VisibilityOf(workspace)), string.Empty, false);
    }

    // A membership counts only while current, undeleted, in a state that grants membership, and held
    // by this caller -- directly or through a role they hold. Its role is judged by the question; an
    // unknown role name resolves to the NotFound option, which grants nothing.
    private static bool InEffectFor(
        WorkspaceMemberConfiguration member, Guid callerUserId, IReadOnlyCollection<Guid> callerRoleIds)
    {
        if (!member.IsCurrent || member.IsDeleted)
            return false;
        if (WorkspaceMemberStates.ByName(member.State) is not { GrantsMembership: true })
            return false;
        return WorkspaceSubjectTypes.ByName(member.SubjectType)
            .Holds(member.SubjectId, callerUserId, callerRoleIds);
    }

    // A workspace with no visibility recorded grants nothing through it -- the same answer as a name
    // this deployment does not know. Ownership, membership and the platform permission still apply.
    private static IWorkspaceVisibility VisibilityOf(IWorkspaceImplementationConfiguration workspace)
        => string.IsNullOrWhiteSpace(workspace.Visibility)
            ? WorkspaceVisibilities.NotFound
            : WorkspaceVisibilities.ByName(workspace.Visibility);

    private async Task<IGenericResult<IReadOnlyCollection<Guid>>> ResolveRoleIds(
        IAuthenticationContext caller, CancellationToken cancellationToken)
    {
        // Read through Get rather than GetAllRoles: GetAllRoles answers an unreadable store with an
        // empty list, which here would read as "this caller holds no roles" and deny for the wrong
        // reason. The refusal is the same either way; what the operator is told is not.
        var all = await _roles.Get(cancellationToken).ConfigureAwait(false);
        if (all.IsFailure || all.Value is null)
            return all.ToNewResult<IReadOnlyCollection<Guid>>();

        return GenericResult<IReadOnlyCollection<Guid>>.Success(
            [.. all.Value
                .Where(r => r.IsCurrent && !r.IsDeleted && caller.Roles.Contains(r.Name, StringComparer.OrdinalIgnoreCase))
                .Select(r => r.Id)]);
    }

    private IGenericResult Deny(string refusalCode, string workspaceName, string reason)
        => GenericResult.Failure(
            WorkspacesResultCodes.ByName(refusalCode), _logger,
            ResultDetails.Create("name", workspaceName, "reason", reason));
}
