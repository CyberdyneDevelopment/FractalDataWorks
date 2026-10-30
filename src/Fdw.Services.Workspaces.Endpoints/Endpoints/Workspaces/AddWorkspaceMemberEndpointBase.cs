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
using Fdw.Services.Authorization;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces.Abstractions;
using Fdw.Services.Workspaces.Results;
using Fdw.Services.Users;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Adds a member to a workspace — grants a user or role a place on it.</summary>
/// <remarks>
/// Inserted directly through the gateway, not by loading the aggregate and re-saving it -- same
/// reason as <see cref="AttachWorkspaceResourceEndpointBase"/>: ImplementationProviderBase.Save's
/// cascade has no update/diff path, so re-saving an aggregate whose Members list is already
/// populated would duplicate every one of them.
/// </remarks>
public abstract class AddWorkspaceMemberEndpointBase
    : CrudCreateEndpointBase<AddWorkspaceMemberRequest, WorkspaceMemberDto>
{
    private readonly WorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _access;
    private readonly IAuthenticationContextAccessor _authContext;

    // Set by CheckExists when it finds a conflict, read back by DuplicateMessage.
    private WorkspaceMemberConfiguration? _conflict;

    /// <inheritdoc />
    protected AddWorkspaceMemberEndpointBase(
        ILogger<AddWorkspaceMemberEndpointBase> logger,
        WorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IAuthenticationContextAccessor authContext) : base(logger)
    {
        _workspaces = workspaces;
        _access = access;
        _authContext = authContext;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "workspaces";

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="IWorkspaceAccessPolicy.MayManageRole"/> decides: the owner, a member whose role
    /// manages members and ranks at or above the role granted, or a holder of <c>workspaces:members</c>
    /// (FDW-1071). <c>workspaces:write</c> does not decide who is in a workspace.
    /// </remarks>
    protected override bool ResourceDecidesAccess => true;

    /// <inheritdoc />
    protected override string Route => "/workspaces/{Name}/members";

    /// <inheritdoc />
    protected override string EndpointSummary => "Add a member to a workspace";

    /// <inheritdoc />
    protected override string EndpointDescription => "Grants a user or role membership of the workspace.";

    /// <inheritdoc />
    protected override string GetResourceName(AddWorkspaceMemberRequest request) => request.Name;

    /// <inheritdoc />
    /// <remarks>
    /// UX_WorkspaceMember_Workspace_Subject_Current is unique on (WorkspaceConfigurationRowId,
    /// SubjectType, SubjectId) WHERE IsCurrent = 1 -- a subject holds at most one role at a time.
    /// </remarks>
    protected override async Task<IGenericResult<bool>> CheckExists(AddWorkspaceMemberRequest request, CancellationToken ct)
    {
        var workspace = await _workspaces.Get(request.Name, ct).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace.ToNewResult<bool>();
        if (workspace.Value is null) return GenericResult<bool>.Success(false);

        // Asked before any conflict is reported: the base answers 409 ahead of Create, and a 409 naming
        // what is already here would confirm a workspace the caller may not see. A refused caller goes
        // on to Create, which refuses them with the policy's own answer (FDW-937).
        var permitted = await _access.MayManageMembers(workspace.Value, ct).ConfigureAwait(false);
        if (WorkspaceAccessRefusals.IsUndetermined(permitted)) return permitted.ToNewResult<bool>();
        if (permitted.IsFailure) return GenericResult<bool>.Success(false);

        // The workspace's members are composed with it: a member row hangs from the workspace by RowId.
        _conflict = workspace.Value.Members.FirstOrDefault(m =>
            string.Equals(m.SubjectType, request.SubjectType, StringComparison.Ordinal)
            && m.SubjectId == request.SubjectId);
        return GenericResult<bool>.Success(_conflict is not null);
    }

    /// <inheritdoc />
    protected override string DuplicateMessage(AddWorkspaceMemberRequest request) =>
        _conflict is null
            ? base.DuplicateMessage(request)
            : $"{_conflict.SubjectType} '{_conflict.SubjectId}' already holds {_conflict.MemberRole} on '{request.Name}'";

    /// <inheritdoc />
    protected override async Task<IGenericResult<WorkspaceMemberDto>> Create(
        AddWorkspaceMemberRequest request, CancellationToken ct)
    {
        if (ReferenceEquals(WorkspaceSubjectTypes.ByName(request.SubjectType), WorkspaceSubjectTypes.NotFound))
        {
            return GenericResult<WorkspaceMemberDto>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceLifecycleValueInvalid"), Logger,
                ResultDetails.Create("name", request.Name, "field", "subjectType", "value", request.SubjectType));
        }

        var role = WorkspaceMemberRoles.ByName(request.MemberRole);
        if (ReferenceEquals(role, WorkspaceMemberRoles.NotFound))
        {
            return GenericResult<WorkspaceMemberDto>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceLifecycleValueInvalid"), Logger,
                ResultDetails.Create("name", request.Name, "field", "memberRole", "value", request.MemberRole));
        }

        var workspace = await _workspaces.Get(request.Name, ct).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace.ToNewResult<WorkspaceMemberDto>();
        if (workspace.Value is null) return WorkspaceAccessRefusals.Absent<WorkspaceMemberDto>(request.Name, Logger);

        var permitted = await _access.MayManageRole(workspace.Value, role, ct).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted.ToNewResult<WorkspaceMemberDto>();

        var subject = await WorkspaceMemberSubjects.Resolve(
            request.Name, request.SubjectType, request.SubjectId,
            Resolve<IUserConfigurationProvider>(), Resolve<IRoleConfigurationProvider>(), Logger, ct).ConfigureAwait(false);
        if (subject.IsFailure) return subject.ToNewResult<WorkspaceMemberDto>();

        if (_authContext.Current is not { } caller || !Guid.TryParse(caller.UserId, out var invitedByUserId))
        {
            return GenericResult<WorkspaceMemberDto>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceOwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "the caller could not be resolved"));
        }

        var joinedAt = DateTimeOffset.UtcNow;
        var member = new WorkspaceMemberConfiguration
        {
            Id = Guid.CreateVersion7(),
            // The column is NOT NULL but carries no identity beyond the subject it names -- the
            // membership IS the row, so its Name is derived from what it grants.
            Name = $"{request.SubjectType}:{request.SubjectId}",
            SubjectType = request.SubjectType,
            SubjectId = request.SubjectId,
            MemberRole = request.MemberRole,
            // Added directly by a caller who manages this workspace's members -- this is a grant,
            // not an invitation, so State starts Active rather than Invited. A2b (FDW-727
            // A4: membership requests) is the separate self-service join flow.
            State = "Active",
            JoinedAt = joinedAt,
            InvitedByUserId = invitedByUserId,
            // Why stamped here: MsSqlInsertTranslator writes every mapped column explicitly, so the
            // unset CLR default (0001-01-01) would overwrite DEFAULT (sysdatetimeoffset()) rather
            // than deferring to it. (FDW-793)
            CreateDate = DateTimeOffset.UtcNow,
        };

        // The member is a child of the workspace aggregate: saving the workspace writes it under the
        // workspace's current row, and the version it replaces keeps the children it had.
        workspace.Value.Members.Add(member);
        var saved = await _workspaces.Update(
            workspace.Value, workspace.Value.Domain, workspace.Value.Implementation, workspace.Value.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure) return saved.ToNewResult<WorkspaceMemberDto>();

        return GenericResult<WorkspaceMemberDto>.Success(new WorkspaceMemberDto
        {
            Id = member.Id,
            SubjectType = member.SubjectType,
            SubjectId = member.SubjectId,
            MemberRole = member.MemberRole,
            State = member.State,
            JoinedAt = member.JoinedAt,
        });
    }
}
