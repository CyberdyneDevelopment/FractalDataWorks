using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces.Abstractions;
using Fdw.Services.Workspaces.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Attaches a resource to a workspace — puts one node on its map.</summary>
/// <remarks>
/// Inserted directly through the gateway rather than by loading the workspace aggregate, mutating
/// its composed Resources list and calling the domain provider's Save: that Save cascades every
/// item currently in the list as a fresh INSERT (ImplementationProviderBase.SaveOneChild has no
/// update/diff path), so re-saving an aggregate that already has resources attached would duplicate
/// every one of them. One row in, one INSERT — the same shape SaveSourceMappingsEndpointBase uses
/// for FieldMapping, and for the same reason.
/// </remarks>
public abstract class AttachWorkspaceResourceEndpointBase
    : CrudCreateEndpointBase<AttachWorkspaceResourceRequest, WorkspaceMapNodeDto>
{
    private readonly WorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _access;
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly IDataSetVariationConfigurationProvider _variations;
    private readonly IAuthenticationContextAccessor _authContext;

    // Set by CheckExists when it finds a conflict, read back by DuplicateMessage -- both run within
    // the same request via CrudCreateEndpointBase.HandleAsync, in that order, on a per-request
    // endpoint instance, so this is never stale or shared across requests.
    private WorkspaceResourceConfiguration? _conflict;

    /// <inheritdoc />
    protected AttachWorkspaceResourceEndpointBase(
        ILogger<AttachWorkspaceResourceEndpointBase> logger,
        WorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IDataSetConfigurationProvider dataSets,
        IDataSetVariationConfigurationProvider variations,
        IAuthenticationContextAccessor authContext) : base(logger)
    {
        _workspaces = workspaces;
        _access = access;
        _dataSets = dataSets;
        _variations = variations;
        _authContext = authContext;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "workspaces";

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="IWorkspaceAccessPolicy.MayWrite"/> decides: the owner, a member whose role may
    /// change it, or a holder of <c>workspaces:write</c> -- so the platform permission is not the gate
    /// in front of it (FDW-936). A data set asks <see cref="IWorkspaceAccessPolicy.MayAttachDataSet"/>
    /// instead, which also needs the caller to see the set and hold a grant for it (FDW-937).
    /// </remarks>
    protected override bool ResourceDecidesAccess => true;

    /// <inheritdoc />
    protected override string Route => "/workspaces/{Name}/resources";

    /// <inheritdoc />
    protected override string EndpointSummary => "Attach a resource to a workspace";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Adds one node to the workspace's map. Returns the created attachment.";

    /// <inheritdoc />
    protected override string GetResourceName(AttachWorkspaceResourceRequest request) => request.Name;

    /// <summary>Resolves a human label for the attached resource, for the kinds that cost one lookup.</summary>
    /// <remarks>
    /// DataSet and DataView are wired. A registered kind that is not wired (DataStore, Pipeline and Calculation
    /// are declared by packages this one does not reference) refuses with <c>WorkspaceResourceKindNotAttachable</c>,
    /// which says so, rather than falling back to the raw resource id as a label — a resource genuinely has no name
    /// yet for kinds this has not been extended to, and inventing one would read as a real label rather than as the
    /// gap it is.
    /// </remarks>
    protected virtual async Task<IGenericResult<string>> ResolveResourceLabel(
        string workspaceName, string resourceType, Guid resourceId, CancellationToken ct)
    {
        if (string.Equals(resourceType, "DataSet", StringComparison.Ordinal))
        {
            var dataSet = await _dataSets.Get(resourceId, ct).ConfigureAwait(false);
            if (dataSet.IsFailure) return dataSet.ToNewResult<string>();
            return dataSet.Value is { Name.Length: > 0 } found
                ? GenericResult<string>.Success(found.Name)
                : GenericResult<string>.Failure(
                    WorkspacesResultCodes.ByName("WorkspaceChildNotFound"), Logger,
                    ResultDetails.Create("name", resourceType, "kind", "resource", "id", resourceId.ToString()));
        }

        if (string.Equals(resourceType, "DataView", StringComparison.Ordinal))
        {
            // A data view is a variation a user owns; one a workspace owns is not a resource to attach.
            var dataView = await _variations.Get(resourceId, ct).ConfigureAwait(false);
            if (dataView.IsFailure) return dataView.ToNewResult<string>();
            return dataView.Value is IDataViewDataSetVariationConfiguration { Name.Length: > 0 } found
                ? GenericResult<string>.Success(found.Name)
                : GenericResult<string>.Failure(
                    WorkspacesResultCodes.ByName("WorkspaceChildNotFound"), Logger,
                    ResultDetails.Create("name", resourceType, "kind", "resource", "id", resourceId.ToString()));
        }

        return GenericResult<string>.Failure(
            WorkspacesResultCodes.ByName("WorkspaceResourceKindNotAttachable"), Logger,
            ResultDetails.Create("name", workspaceName, "kind", resourceType));
    }

    /// <inheritdoc />
    /// <remarks>
    /// UX_WorkspaceResource_Workspace_Type_Resource_Current is unique on (WorkspaceConfigurationRowId,
    /// ResourceType, ResourceId) WHERE IsCurrent = 1 -- the same resource cannot be attached to the
    /// same workspace twice, under any relationship. A prior version of this comment said otherwise;
    /// the schema was always the truth and the comment was wrong, not the constraint.
    /// </remarks>
    protected override async Task<IGenericResult<bool>> CheckExists(AttachWorkspaceResourceRequest request, CancellationToken ct)
    {
        var workspace = await _workspaces.Get(request.Name, ct).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace.ToNewResult<bool>();
        if (workspace.Value is null) return GenericResult<bool>.Success(false);

        // Asked before any conflict is reported: the base answers 409 ahead of Create, and a 409 naming
        // what is already here would confirm a workspace the caller may not see. A refused caller goes
        // on to Create, which refuses them with the policy's own answer (FDW-937).
        var permitted = await _access.MayWrite(workspace.Value, ct).ConfigureAwait(false);
        if (WorkspaceAccessRefusals.IsUndetermined(permitted)) return permitted.ToNewResult<bool>();
        if (permitted.IsFailure) return GenericResult<bool>.Success(false);

        // The workspace's resources are composed with it: a resource row hangs from the workspace by RowId.
        _conflict = workspace.Value.Resources.FirstOrDefault(r =>
            string.Equals(r.ResourceType, request.ResourceType, StringComparison.Ordinal)
            && r.ResourceId == request.ResourceId);
        return GenericResult<bool>.Success(_conflict is not null);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The generic base wording ("A workspaces with this name already exists") is CrudCreate's
    /// duplicate-by-name text, and this endpoint's conflict isn't about the workspace's name at
    /// all -- it's a resource already attached under another relationship. _conflict's Name is
    /// already the resolved data set label (stamped when it was first attached), so naming it here
    /// costs nothing extra.
    /// </remarks>
    protected override string DuplicateMessage(AttachWorkspaceResourceRequest request) =>
        _conflict is null
            ? base.DuplicateMessage(request)
            : $"'{_conflict.Name}' is already attached to '{request.Name}' as {_conflict.Relationship}";

    /// <inheritdoc />
    protected override async Task<IGenericResult<WorkspaceMapNodeDto>> Create(
        AttachWorkspaceResourceRequest request, CancellationToken ct)
    {
        if (ReferenceEquals(WorkspaceResourceKinds.ByName(request.ResourceType), WorkspaceResourceKinds.NotFound))
        {
            return GenericResult<WorkspaceMapNodeDto>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceResourceKindUnknown"), Logger,
                ResultDetails.Create(
                    "name", request.Name,
                    "value", request.ResourceType,
                    "kinds", string.Join(", ", WorkspaceResourceKinds.All().Select(k => k.Name).OrderBy(n => n, StringComparer.Ordinal))));
        }

        if (ReferenceEquals(WorkspaceResourceRelationships.ByName(request.Relationship), WorkspaceResourceRelationships.NotFound))
        {
            return GenericResult<WorkspaceMapNodeDto>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceLifecycleValueInvalid"), Logger,
                ResultDetails.Create("name", request.Name, "field", "relationship", "value", request.Relationship ?? string.Empty));
        }

        var workspace = await _workspaces.Get(request.Name, ct).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace.ToNewResult<WorkspaceMapNodeDto>();
        if (workspace.Value is null) return WorkspaceAccessRefusals.Absent<WorkspaceMapNodeDto>(request.Name, Logger);

        var permitted = string.Equals(request.ResourceType, "DataSet", StringComparison.Ordinal)
            ? await _access.MayAttachDataSet(workspace.Value, request.ResourceId, ct).ConfigureAwait(false)
            : await _access.MayWrite(workspace.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted.ToNewResult<WorkspaceMapNodeDto>();

        var label = await ResolveResourceLabel(request.Name, request.ResourceType, request.ResourceId, ct).ConfigureAwait(false);
        if (label.IsFailure) return label.ToNewResult<WorkspaceMapNodeDto>();

        if (_authContext.Current is not { } caller || !Guid.TryParse(caller.UserId, out var addedByUserId))
        {
            return GenericResult<WorkspaceMapNodeDto>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceOwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "the caller could not be resolved"));
        }

        var resource = new WorkspaceResourceConfiguration
        {
            Id = Guid.CreateVersion7(),
            // The column is NOT NULL but carries no identity of its own beyond the resource it
            // names — the attachment IS the row, so its Name is the resource's own label.
            Name = label.Value!,
            ResourceType = request.ResourceType,
            ResourceId = request.ResourceId,
            Relationship = request.Relationship,
            AddedByUserId = addedByUserId,
            // Why stamped here: MsSqlInsertTranslator writes every mapped column explicitly, so the
            // unset CLR default (0001-01-01) would overwrite DEFAULT (sysdatetimeoffset()) rather
            // than deferring to it. (FDW-793)
            CreateDate = DateTimeOffset.UtcNow,
        };

        // The resource is a child of the workspace aggregate: saving the workspace writes it under the
        // workspace's current row, and the version it replaces keeps the children it had.
        workspace.Value.Resources.Add(resource);
        var saved = await _workspaces.Update(
            workspace.Value, workspace.Value.Domain, workspace.Value.Implementation, workspace.Value.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure) return saved.ToNewResult<WorkspaceMapNodeDto>();

        return GenericResult<WorkspaceMapNodeDto>.Success(new WorkspaceMapNodeDto
        {
            Id = resource.Id.ToString(),
            Label = resource.Name,
            NodeType = resource.ResourceType,
            Category = resource.Relationship,
            X = null,
            Y = null,
            Metadata = new System.Collections.Generic.Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["resourceId"] = resource.ResourceId.ToString(),
                ["addedByUserId"] = resource.AddedByUserId.ToString(),
            },
        });
    }
}
