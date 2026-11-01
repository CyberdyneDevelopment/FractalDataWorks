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
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces.Abstractions;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Sets or changes a relationship's join fields — the one thing create can leave unresolved.</summary>
/// <remarks>
/// A relationship can be declared before anyone has picked which columns carry it (both
/// LeftFieldId/RightFieldId are nullable on the row), and until FDW-794's addendum there was no way
/// to name them afterward. Version-on-write through the workspace aggregate's save, same as
/// create and detach on this domain, carrying every unrelated column forward from the row already
/// loaded by <see cref="FindForUpdate"/> rather than reconstructing it from the request.
/// </remarks>
public abstract class UpdateWorkspaceRelationshipEndpointBase
    : CrudUpdateEndpointBase<UpdateWorkspaceRelationshipRequest, WorkspaceMapEdgeDto>
{
    private readonly WorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _access;
    private readonly IDataSetConfigurationProvider _dataSets;

    // Set by FindForUpdate, read by Update -- both run within the same request via
    // CrudUpdateEndpointBase.HandleAsync, in that order, on a per-request endpoint instance.
    private WorkspaceRelationshipConfiguration? _existing;
    private IWorkspaceImplementationConfiguration? _workspace;

    /// <inheritdoc />
    protected UpdateWorkspaceRelationshipEndpointBase(
        ILogger<UpdateWorkspaceRelationshipEndpointBase> logger,
        WorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IDataSetConfigurationProvider dataSets) : base(logger)
    {
        _workspaces = workspaces;
        _access = access;
        _dataSets = dataSets;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "workspaces";

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="IWorkspaceAccessPolicy.MayWrite"/> decides: the owner, a member whose role may
    /// change it, or a holder of <c>workspaces:write</c> -- so the platform permission is not the gate
    /// in front of it (FDW-936).
    /// </remarks>
    protected override bool ResourceDecidesAccess => true;

    /// <inheritdoc />
    protected override string Route => "/workspaces/{Name}/relationships/{RelationshipId}";

    /// <inheritdoc />
    protected override string EndpointSummary => "Set a relationship's join fields";

    /// <inheritdoc />
    protected override string EndpointDescription => "Changes which fields two related data sets join on.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(UpdateWorkspaceRelationshipRequest request)
        => request.RelationshipId.ToString();

    /// <inheritdoc />
    protected override async Task<IGenericResult<WorkspaceMapEdgeDto?>> FindForUpdate(
        UpdateWorkspaceRelationshipRequest request, CancellationToken ct)
    {
        var workspace = await _workspaces.Get(request.Name, ct).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace.ToNewResult<WorkspaceMapEdgeDto?>();
        if (workspace.Value is null) return GenericResult<WorkspaceMapEdgeDto?>.Success(null);

        var permitted = await _access.MayWrite(workspace.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted.ToNewResult<WorkspaceMapEdgeDto?>();

        // Found in the workspace's composed relationships. The aggregate is kept for the write: the change
        // is saved with the workspace, which writes its children under the workspace's current row.
        _workspace = workspace.Value;
        _existing = workspace.Value.Relationships.FirstOrDefault(x => x.Id == request.RelationshipId);
        return GenericResult<WorkspaceMapEdgeDto?>.Success(_existing is null ? null : ToDto(_existing));
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<WorkspaceMapEdgeDto>> Update(
        UpdateWorkspaceRelationshipRequest request, WorkspaceMapEdgeDto existing, CancellationToken ct)
    {
        // FindForUpdate always runs before Update in CrudUpdateEndpointBase.HandleAsync, and a null
        // _existing there already short-circuits to 404 before Update is ever called.
        var row = _existing!;

        var leftField = await WorkspaceFieldOwnershipValidator.Validate(
            _dataSets, Logger, request.Name, "left", row.LeftDataSetId, request.LeftFieldId, ct).ConfigureAwait(false);
        if (leftField.IsFailure) return leftField.ToNewResult<WorkspaceMapEdgeDto>();

        var rightField = await WorkspaceFieldOwnershipValidator.Validate(
            _dataSets, Logger, request.Name, "right", row.RightDataSetId, request.RightFieldId, ct).ConfigureAwait(false);
        if (rightField.IsFailure) return rightField.ToNewResult<WorkspaceMapEdgeDto>();

        row.LeftFieldId = request.LeftFieldId;
        row.RightFieldId = request.RightFieldId;
        // Why stamped here: ConfigurationSaveCommand's insert writes every mapped
        // column explicitly, same as the create/attach sites (FDW-793) -- CreateDate is left as the
        // value already on `row` (the original creation date, not this version's), and only
        // ModifyDate moves.
        row.ModifyDate = DateTimeOffset.UtcNow;

        // row is the instance inside the aggregate's Relationships, so the aggregate already carries the change.
        var saved = await _workspaces.Update(_workspace!, _workspace!.Domain, _workspace.Implementation, _workspace.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure) return saved.ToNewResult<WorkspaceMapEdgeDto>();

        return GenericResult<WorkspaceMapEdgeDto>.Success(ToDto(row));
    }

    private static WorkspaceMapEdgeDto ToDto(WorkspaceRelationshipConfiguration rel) => new()
    {
        Id = rel.Id.ToString(),
        Source = rel.LeftDataSetId.ToString(),
        Target = rel.RightDataSetId.ToString(),
        RelationType = rel.Cardinality,
        // Matches GetWorkspaceMapEndpointBase's Label = rel.Name -- the row's own name, not the
        // create request's optional label, since an unlabelled relationship's Name is its own id
        // string and that IS what the map shows for it.
        Label = rel.Name,
        IsDefined = rel.LeftFieldId.HasValue && rel.RightFieldId.HasValue,
        Metadata = BuildEdgeMetadata(rel),
    };

    private static Dictionary<string, object> BuildEdgeMetadata(WorkspaceRelationshipConfiguration rel)
    {
        var metadata = new Dictionary<string, object>(StringComparer.Ordinal);
        if (rel.LeftFieldId is { } left) metadata["leftFieldId"] = left.ToString();
        if (rel.RightFieldId is { } right) metadata["rightFieldId"] = right.ToString();
        return metadata;
    }
}
