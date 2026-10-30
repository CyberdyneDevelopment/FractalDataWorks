using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces.Abstractions;
using Fdw.Services.Workspaces.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Asks to join a workspace, for the caller — honours the workspace's own <see cref="IWorkspaceImplementationConfiguration.JoinPolicy"/>.</summary>
/// <remarks>
/// The request is always for the caller, as a User (FDW-1070): naming another subject is member
/// management, which this endpoint never does. An AutoApprove workspace grants the role it states in
/// <see cref="IWorkspaceImplementationConfiguration.AutoApproveRole"/>, never the role asked for, so
/// whoever can find the workspace cannot make themselves its Owner.
/// <para>
/// Requesting is the thing someone without write access does, so this never calls MayWrite. It asks
/// <see cref="IWorkspaceAccessPolicy.MaySee"/> instead: you can ask to join only a workspace you can
/// find, and a Private one you cannot see answers as if it did not exist, so its name cannot be
/// confirmed by requesting to join it (FDW-936). A Closed policy is a business refusal read off the
/// workspace's own configuration, not an authorization decision.
/// </para>
/// </remarks>
public abstract class RequestWorkspaceMembershipEndpointBase
    : CrudCreateEndpointBase<RequestWorkspaceMembershipRequest, WorkspaceMembershipRequestDto>
{
    private readonly WorkspaceConfigurationProvider _workspaces;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly IWorkspaceAccessPolicy _access;

    // Set by CheckExists when it finds a conflict, read back by DuplicateMessage.
    private WorkspaceMembershipRequestConfiguration? _conflict;

    /// <inheritdoc />
    protected RequestWorkspaceMembershipEndpointBase(
        ILogger<RequestWorkspaceMembershipEndpointBase> logger,
        WorkspaceConfigurationProvider workspaces,
        IAuthenticationContextAccessor authContext,
        IWorkspaceAccessPolicy access) : base(logger)
    {
        _workspaces = workspaces;
        _authContext = authContext;
        _access = access;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "workspaces";

    /// <inheritdoc />
    /// <remarks>Anyone signed in may ask to join a workspace they can see; <see cref="IWorkspaceAccessPolicy.MaySee"/> decides (FDW-936).</remarks>
    protected override bool ResourceDecidesAccess => true;

    /// <inheritdoc />
    protected override string Route => "/workspaces/{Name}/membership-requests";

    /// <inheritdoc />
    protected override string EndpointSummary => "Ask to join a workspace";

    /// <inheritdoc />
    protected override string EndpointDescription => "Requests membership, honouring the workspace's join policy.";

    /// <inheritdoc />
    protected override string GetResourceName(RequestWorkspaceMembershipRequest request) => request.Name;

    /// <inheritdoc />
    /// <remarks>
    /// A caller with an already-Pending request cannot ask again until it is reviewed. Asked only of a
    /// workspace the caller may see: a 409 for one they may not would confirm its name (FDW-1079).
    /// </remarks>
    protected override async Task<IGenericResult<bool>> CheckExists(RequestWorkspaceMembershipRequest request, CancellationToken ct)
    {
        if (CallerUserId() is not { } callerUserId) return GenericResult<bool>.Success(false);

        var workspace = await _workspaces.Get(request.Name, ct).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace.ToNewResult<bool>();
        if (workspace.Value is null) return GenericResult<bool>.Success(false);

        var sees = await _access.MaySee(workspace.Value, ct).ConfigureAwait(false);
        if (WorkspaceAccessRefusals.IsUndetermined(sees)) return sees.ToNewResult<bool>();
        if (sees.IsFailure) return GenericResult<bool>.Success(false);

        // The workspace's requests are composed with it, current versions only.
        _conflict = workspace.Value.MembershipRequests.FirstOrDefault(r =>
            ReferenceEquals(WorkspaceSubjectTypes.ByName(r.SubjectType), WorkspaceSubjectTypes.User)
            && r.SubjectId == callerUserId
            && string.Equals(r.Status, "Pending", StringComparison.Ordinal));
        return GenericResult<bool>.Success(_conflict is not null);
    }

    // The caller as a User subject, or null when there is no caller or its UserId is not a Guid.
    private Guid? CallerUserId()
        => _authContext.Current is { } caller && Guid.TryParse(caller.UserId, out var userId) ? userId : null;

    /// <inheritdoc />
    protected override string DuplicateMessage(RequestWorkspaceMembershipRequest request) =>
        _conflict is null
            ? base.DuplicateMessage(request)
            : $"{_conflict.SubjectType} '{_conflict.SubjectId}' already has a pending request on '{request.Name}'";

    /// <summary>Validates RequestedRole against its closed set.</summary>
    private IGenericResult ValidateFields(RequestWorkspaceMembershipRequest request)
    {
        if (ReferenceEquals(WorkspaceMemberRoles.ByName(request.RequestedRole), WorkspaceMemberRoles.NotFound))
        {
            return GenericResult.Failure(
                WorkspacesResultCodes.ByName("WorkspaceLifecycleValueInvalid"), Logger,
                ResultDetails.Create("name", request.Name, "field", "requestedRole", "value", request.RequestedRole));
        }

        return GenericResult.Success();
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<WorkspaceMembershipRequestDto>> Create(
        RequestWorkspaceMembershipRequest request, CancellationToken ct)
    {
        var validated = ValidateFields(request);
        if (validated.IsFailure) return validated.ToNewResult<WorkspaceMembershipRequestDto>();

        var workspace = await _workspaces.Get(request.Name, ct).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace.ToNewResult<WorkspaceMembershipRequestDto>();
        if (workspace.Value is null) return WorkspaceAccessRefusals.Absent<WorkspaceMembershipRequestDto>(request.Name, Logger);

        var sees = await _access.MaySee(workspace.Value, ct).ConfigureAwait(false);
        if (sees.IsFailure) return sees.ToNewResult<WorkspaceMembershipRequestDto>();

        var policy = WorkspaceJoinPolicies.ByName(workspace.Value.JoinPolicy);
        if (ReferenceEquals(policy, WorkspaceJoinPolicies.NotFound) || !policy.AcceptsRequests)
        {
            return GenericResult<WorkspaceMembershipRequestDto>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceJoinPolicyRefused"), Logger,
                ResultDetails.Create("name", request.Name, "reason", $"'{workspace.Value.JoinPolicy}' does not accept membership requests"));
        }

        if (CallerUserId() is not { } requestedByUserId)
        {
            return GenericResult<WorkspaceMembershipRequestDto>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceOwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "the caller could not be resolved"));
        }

        var autoApproved = policy.AutoApproves;
        var grantable = AutoApprovalGrantable(workspace.Value, autoApproved);
        if (grantable.IsFailure) return grantable.ToNewResult<WorkspaceMembershipRequestDto>();

        var now = DateTimeOffset.UtcNow;
        var membershipRequest = BuildRequestRow(request, workspace.Value.Id, requestedByUserId, autoApproved, now);

        // The request, and the membership an auto-approval grants, are children of the workspace
        // aggregate: one save writes both under the workspace's current row.
        workspace.Value.MembershipRequests.Add(membershipRequest);
        if (autoApproved)
            WorkspaceMembershipGrant.Grant(workspace.Value, membershipRequest, workspace.Value.AutoApproveRole!, requestedByUserId, now);

        var saved = await _workspaces.Update(
            workspace.Value, workspace.Value.Domain, workspace.Value.Implementation, workspace.Value.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure) return saved.ToNewResult<WorkspaceMembershipRequestDto>();

        return GenericResult<WorkspaceMembershipRequestDto>.Success(WorkspaceMembershipRequestDtos.From(membershipRequest));
    }

    // Stored before AutoApproveRole was validated on write, a row may still lack a usable one: refused
    // with the reason rather than granting the role asked for (FDW-1070). Nothing to check otherwise.
    private IGenericResult AutoApprovalGrantable(IWorkspaceImplementationConfiguration workspace, bool autoApproved)
        => autoApproved
            ? WorkspaceLifecycleValidator.ValidateAutoApproveRole(workspace.Name, workspace.JoinPolicy, workspace.AutoApproveRole, Logger)
            : GenericResult.Success();

    private static WorkspaceMembershipRequestConfiguration BuildRequestRow(
        RequestWorkspaceMembershipRequest request, Guid workspaceImplementationId, Guid requestedByUserId, bool autoApproved, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = $"{WorkspaceSubjectTypes.User.Name}:{requestedByUserId}:{workspaceImplementationId}",
        RequestedByUserId = requestedByUserId,
        SubjectType = WorkspaceSubjectTypes.User.Name,
        SubjectId = requestedByUserId,
        RequestedRole = request.RequestedRole,
        Justification = request.Justification,
        Status = autoApproved ? "Approved" : "Pending",
        ReviewedByUserId = autoApproved ? requestedByUserId : null,
        ReviewedAt = autoApproved ? now : null,
        ReviewNotes = autoApproved ? "Auto-approved by the workspace's join policy" : null,
        // Why stamped here: MsSqlInsertTranslator writes every mapped column explicitly, so the
        // unset CLR default (0001-01-01) would overwrite DEFAULT (sysdatetimeoffset()) rather
        // than deferring to it. (FDW-793)
        CreateDate = now,
    };
}
