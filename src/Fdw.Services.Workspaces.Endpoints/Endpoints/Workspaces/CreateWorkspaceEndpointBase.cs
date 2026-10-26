using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions.Results;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Workspaces.Logging;
using Fdw.Services.Workspaces.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Creates a workspace.</summary>
public abstract class CreateWorkspaceEndpointBase : CrudCreateEndpointBase<CreateWorkspaceRequest, WorkspaceDetailResponse>
{
    private readonly IWorkspaceConfigurationProvider _provider;

    /// <inheritdoc />
    private readonly IAuthenticationContextAccessor _authContext;

    /// <inheritdoc />
    protected CreateWorkspaceEndpointBase(
        ILogger<CreateWorkspaceEndpointBase> logger,
        IWorkspaceConfigurationProvider provider,
        IAuthenticationContextAccessor authContext) : base(logger)
    {
        _provider = provider;
        _authContext = authContext;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "workspaces";

    /// <inheritdoc />
    protected override string GetResourceName(CreateWorkspaceRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExists(CreateWorkspaceRequest request, CancellationToken ct)
    {
        var result = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return result.IsFailure
            ? result.ToNewResult<bool>()
            : GenericResult<bool>.Success(result.Value is not null);
    }

    /// <summary>
    /// Rejects a Status, Visibility or JoinPolicy that is not a registered option, and an AutoApproveRole
    /// that does not fit the join policy.
    /// </summary>
    /// <remarks>
    /// All three are required on create, and an empty string is not a registered option, so the
    /// same check that rejects a typo also rejects an omission. Nothing is substituted: a project
    /// silently created Private and Closed, or silently Open, is a decision the caller did not make.
    /// </remarks>
    /// <param name="request">The create request.</param>
    protected virtual IGenericResult ValidateLifecycle(CreateWorkspaceRequest request)
    {
        var status = WorkspaceLifecycleValidator.ValidateStatus(request.Name, request.Status, Logger);
        if (status.IsFailure) return status;

        var visibility = WorkspaceLifecycleValidator.ValidateVisibility(request.Name, request.Visibility, Logger);
        if (visibility.IsFailure) return visibility;

        var joinPolicy = WorkspaceLifecycleValidator.ValidateJoinPolicy(request.Name, request.JoinPolicy, Logger);
        return joinPolicy.IsFailure
            ? joinPolicy
            : WorkspaceLifecycleValidator.ValidateAutoApproveRole(request.Name, request.JoinPolicy, request.AutoApproveRole, Logger);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<WorkspaceDetailResponse>> Create(
        CreateWorkspaceRequest request, CancellationToken ct)
    {
        var lifecycle = ValidateLifecycle(request);
        if (lifecycle.IsFailure) return lifecycle.ToNewResult<WorkspaceDetailResponse>();

        // Why Guid.CreateVersion7: the database has no DEFAULT on Id and never mints one. A
        // time-ordered id also keeps insert order and sort order the same thing.
        // Why this refuses rather than defaulting: OwnerUserId is a Guid and NOT NULL, so an
        // unresolved caller lands on Guid.Empty, stores cleanly, and reads back as an owner nobody
        // can resolve. Ownership is also what workspaces:write is to be scoped by (FDW-725), so a
        // workspace created with no real owner is one nobody can edit once that lands -- including
        // whoever created it. There are two ways to have no owner and neither is defaultable: no
        // authentication context at all, and a UserId that is not a Guid.
        if (_authContext.Current is not { } caller)
        {
            return GenericResult<WorkspaceDetailResponse>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceOwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "no authentication context"));
        }

        if (!Guid.TryParse(caller.UserId, out var ownerUserId))
        {
            return GenericResult<WorkspaceDetailResponse>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceOwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "UserId is not a Guid"));
        }

        var classification = DataClassificationValidator.Validate(request.ClassificationId, Logger);
        if (classification.IsFailure) return classification.ToNewResult<WorkspaceDetailResponse>();

        // The workspace is written in the caller's active tenant, so that is the tenant its organization must be one of.
        if (request.OrganizationId is { } organizationId)
        {
            var inTenant = await WorkspaceOrganizationCheck.Check(
                Resolve<IOrganizationProvider>(), request.Name, organizationId, caller.ActiveTenantId, Logger, ct).ConfigureAwait(false);
            if (inTenant.IsFailure) return inTenant.ToNewResult<WorkspaceDetailResponse>();
        }

        var config = new WorkspaceImplementationConfiguration
        {
            Id = Guid.CreateVersion7(),
            // The implementation the domain provider is registered under. A new record names it or the
            // save refuses it: Save looks the name up before writing, and '' is registered for nothing.
            Implementation = "Workspace",
            // Why stamped here rather than left to the column DEFAULT: MsSqlInsertTranslator writes
            // every mapped column explicitly, including ones nobody set, so the unset CLR default
            // (0001-01-01) overwrites DEFAULT (sysdatetimeoffset()) rather than deferring to it. Same
            // reason CreateWorkspaceNoteEndpointBase already stamps its own CreateDate. (FDW-793)
            CreateDate = DateTimeOffset.UtcNow,
            OwnerUserId = ownerUserId,
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            ClassificationId = request.ClassificationId,
            Purpose = request.Purpose,
            Status = request.Status,
            Visibility = request.Visibility,
            JoinPolicy = request.JoinPolicy,
            AutoApproveRole = request.AutoApproveRole,
            StandInSeed = request.StandInSeed,
            OrganizationId = request.OrganizationId,
        };

        // Resolved before anything is written, so a host that has not registered it refuses cleanly
        // instead of saving a workspace it then cannot give permissions (FDW-937).
        if (TryResolve<IWorkspacePermissions>() is not { } permissions)
        {
            return GenericResult<WorkspaceDetailResponse>.Failure(
                WorkspacesResultCodes.ByName("WorkspacePermissionsUnavailable"), Logger,
                ResultDetails.Create("name", request.Name, "step", "created"));
        }

        var saved = await _provider.Save(config, "Workspace", config.Implementation, config.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure) return saved.ToNewResult<WorkspaceDetailResponse>();

        var permitted = await CreatePermissions(permissions, config, ct).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted.ToNewResult<WorkspaceDetailResponse>();

        // The owning tenant is the one the row was written in, which the database stamps from the session, so it is
        // read back rather than assumed (FDW-1101).
        var stored = await _provider.Get(config.Id, ct).ConfigureAwait(false);
        if (stored.IsFailure) return stored.ToNewResult<WorkspaceDetailResponse>();
        if (stored.Value is null)
        {
            return GenericResult<WorkspaceDetailResponse>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", request.Name));
        }

        var ownership = await WorkspaceOwnershipReader.Read(Resolve<IOrganizationProvider>(), stored.Value, Logger, ct).ConfigureAwait(false);
        return ownership.IsFailure
            ? ownership.ToNewResult<WorkspaceDetailResponse>()
            : GenericResult<WorkspaceDetailResponse>.Success(WorkspaceResponseMapper.ToDetail(stored.Value, ownership.Value!));
    }

    /// <summary>Creates the workspace's own permissions, undoing the workspace if that fails.</summary>
    /// <remarks>
    /// A workspace without its permissions cannot be granted to anyone by role (FDW-937), so it is not
    /// left behind half-made. The undo tries both of its steps whatever the other did -- removing
    /// whichever permission was made, and deleting the workspace -- because neither depends on the
    /// other. Should either fail, the result names the undo that failed so it can be repaired by hand.
    /// </remarks>
    private async Task<IGenericResult> CreatePermissions(
        IWorkspacePermissions permissions, WorkspaceImplementationConfiguration config, CancellationToken ct)
    {
        var created = await permissions.Create(config.Id, config.Name, ct).ConfigureAwait(false);
        if (created.IsSuccess) return created;

        WorkspaceLifecycleLog.UndoingCreate(Logger, config.Name, string.Join(" <- ", created.CodeChain.Select(code => code.Name)));
        var retired = await permissions.Remove(config.Id, ct).ConfigureAwait(false);
        var deleted = await _provider.Delete(config.Id, ct).ConfigureAwait(false);
        if (retired.IsSuccess && deleted.IsSuccess)
        {
            WorkspaceLifecycleLog.CreateUndone(Logger, config.Name);
            return created;
        }

        var id = config.Id.ToString("D", CultureInfo.InvariantCulture);
        var left = (retired.IsSuccess, deleted.IsSuccess) switch
        {
            (false, false) => $"the workspace and its permissions workspaces/{id}:*",
            (false, true) => $"its permissions workspaces/{id}:*",
            _ => "the workspace",
        };
        return GenericResult.Failure(
            WorkspacesResultCodes.ByName("WorkspaceCompensationFailed"), Logger,
            ResultDetails.Create("name", config.Name, "step", "creating its permissions", "undo", left));
    }
}
