using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions.Results;
using Fdw.Data.Abstractions;
using Fdw.Services.Workspaces.Abstractions;
using Fdw.Results;
using Fdw.Services.Workspaces.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Modifies a workspace.</summary>
/// <remarks>
/// A null field on the request means "leave it alone", which is what PATCH means. The existing
/// configuration is re-read and only the supplied fields are moved, so an omitted Status is never
/// mistaken for a request to blank it.
/// </remarks>
public abstract class UpdateWorkspaceEndpointBase : CrudUpdateEndpointBase<UpdateWorkspaceRequest, WorkspaceDetailResponse>
{
    private readonly IWorkspaceConfigurationProvider _provider;
    private readonly IWorkspaceAccessPolicy _access;

    /// <inheritdoc />
    protected UpdateWorkspaceEndpointBase(
        ILogger<UpdateWorkspaceEndpointBase> logger,
        IWorkspaceConfigurationProvider provider,
        IWorkspaceAccessPolicy access) : base(logger)
    {
        _provider = provider;
        _access = access;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "workspaces";

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="IWorkspaceAccessPolicy.MayWrite"/> decides: the owner, a member whose role may
    /// change it, or a holder of <c>workspaces:write</c> -- so the platform permission is not the gate
    /// in front of it (FDW-936).
    /// </remarks>
    protected override bool ResourceDecidesAccess => true;

    /// <inheritdoc />
    protected override string GetResourceIdentifier(UpdateWorkspaceRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<WorkspaceDetailResponse?>> FindForUpdate(
        UpdateWorkspaceRequest request, CancellationToken ct)
    {
        var result = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<WorkspaceDetailResponse?>();
        if (result.Value is null) return GenericResult<WorkspaceDetailResponse?>.Success(null);

        var ownership = await WorkspaceOwnershipReader.Read(Resolve<IOrganizationProvider>(), result.Value, Logger, ct).ConfigureAwait(false);
        return ownership.IsFailure
            ? ownership.ToNewResult<WorkspaceDetailResponse?>()
            : GenericResult<WorkspaceDetailResponse?>.Success(WorkspaceResponseMapper.ToDetail(result.Value, ownership.Value!));
    }

    /// <summary>
    /// Rejects a supplied Status, Visibility or JoinPolicy that is not a registered option.
    /// </summary>
    /// <remarks>
    /// Only what was supplied is checked. A null field means "leave it alone", so validating it
    /// would reject every PATCH that does not restate all three.
    /// </remarks>
    /// <param name="request">The update request.</param>
    protected virtual IGenericResult ValidateLifecycle(UpdateWorkspaceRequest request)
    {
        var status = WorkspaceLifecycleValidator.ValidateStatus(request.Name, request.Status, Logger);
        if (status.IsFailure) return status;

        var visibility = WorkspaceLifecycleValidator.ValidateVisibility(request.Name, request.Visibility, Logger);
        return visibility.IsFailure
            ? visibility
            : WorkspaceLifecycleValidator.ValidateJoinPolicy(request.Name, request.JoinPolicy, Logger);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<WorkspaceDetailResponse>> Update(
        UpdateWorkspaceRequest request, WorkspaceDetailResponse existing, CancellationToken ct)
    {
        var lifecycle = ValidateLifecycle(request);
        if (lifecycle.IsFailure) return lifecycle.ToNewResult<WorkspaceDetailResponse>();

        var current = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        if (current.IsFailure) return current.ToNewResult<WorkspaceDetailResponse>();

        if (current.Value is null)
        {
            return GenericResult<WorkspaceDetailResponse>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", request.Name));
        }

        var config = current.Value;

        // After the load, because the decision is about THIS workspace: who owns it and who is in
        // it are on the row. workspaces:write got the caller this far and says nothing about which
        // workspace they may change.
        var permitted = await _access.MayWrite(config, ct).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted.ToNewResult<WorkspaceDetailResponse>();

        var classification = DataClassificationValidator.Validate(request.ClassificationId, Logger);
        if (classification.IsFailure) return classification.ToNewResult<WorkspaceDetailResponse>();

        var inTenant = await CheckOrganization(request, config, ct).ConfigureAwait(false);
        if (inTenant.IsFailure) return inTenant.ToNewResult<WorkspaceDetailResponse>();

        ApplyFields(config, request);

        // The role travels with the policy it is stored under: kept while the policy still auto-approves,
        // cleared when it no longer does, and validated against the policy the row will have (FDW-1070).
        var joinPolicy = request.JoinPolicy ?? config.JoinPolicy;
        var autoApproveRole = request.AutoApproveRole
            ?? (WorkspaceJoinPolicies.ByName(joinPolicy).AutoApproves ? config.AutoApproveRole : null);
        var autoApprove = WorkspaceLifecycleValidator.ValidateAutoApproveRole(request.Name, joinPolicy, autoApproveRole, Logger);
        if (autoApprove.IsFailure) return autoApprove.ToNewResult<WorkspaceDetailResponse>();

        config.JoinPolicy = joinPolicy;
        config.AutoApproveRole = autoApproveRole;
        config.StandInSeed = request.StandInSeed ?? config.StandInSeed;

        // This is a plain field-only PATCH -- Members/Resources/Relationships are never touched
        // here (they have their own dedicated attach/detach/add/remove/create/update endpoints,
        // which write directly through the gateway). _provider.Save's cascade would otherwise
        // walk whatever Get() composed onto these collections and re-INSERT every one of them on
        // every unrelated field change (FDW-801) -- an unnecessary re-version of unrelated rows
        // at best, a lost-update race against a concurrent child edit at worst. Cleared for the
        // save and restored immediately after, so the response DTO still reflects them.
        var members = config.Members;
        var resources = config.Resources;
        var relationships = config.Relationships;
        config.Members = [];
        config.Resources = [];
        config.Relationships = [];

        var saved = await _provider.Save(config, "Workspace", config.Implementation, config.Name, ct).ConfigureAwait(false);

        config.Members = members;
        config.Resources = resources;
        config.Relationships = relationships;

        if (saved.IsFailure) return saved.ToNewResult<WorkspaceDetailResponse>();

        var ownership = await WorkspaceOwnershipReader.Read(Resolve<IOrganizationProvider>(), config, Logger, ct).ConfigureAwait(false);
        return ownership.IsFailure
            ? ownership.ToNewResult<WorkspaceDetailResponse>()
            : GenericResult<WorkspaceDetailResponse>.Success(WorkspaceResponseMapper.ToDetail(config, ownership.Value!));
    }

    // The organization a workspace is moved to must be one of its own tenant; leaving it out, or naming the one it is in, asks nothing (FDW-2338).
    private async Task<IGenericResult> CheckOrganization(
        UpdateWorkspaceRequest request, IWorkspaceImplementationConfiguration config, CancellationToken ct)
    {
        if (request.OrganizationId is not { } organizationId || organizationId == config.OrganizationId)
            return GenericResult.Success();

        return await WorkspaceOrganizationCheck.Check(
            Resolve<IOrganizationProvider>(), request.Name, organizationId, config.TenantId, Logger, ct).ConfigureAwait(false);
    }

    // The plain fields this PATCH carries. Left out means unchanged, as for every field this update carries.
    // The join policy, its auto-approve role and the stand-in seed stay in Update: the first two are validated
    // together against the policy the row will have, and a refusal there must leave the row as it was read.
    private static void ApplyFields(IWorkspaceImplementationConfiguration config, UpdateWorkspaceRequest request)
    {
        config.DisplayName = request.DisplayName ?? config.DisplayName;
        config.Description = request.Description ?? config.Description;

        if (request.ClassificationId is { } classificationId)
        {
            config.ClassificationId = classificationId;
        }

        config.Purpose = request.Purpose ?? config.Purpose;
        config.OrganizationId = request.OrganizationId ?? config.OrganizationId;
        config.Status = request.Status ?? config.Status;
        config.Visibility = request.Visibility ?? config.Visibility;
    }
}
