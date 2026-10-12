using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Workspaces.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Declares a relationship between two of a workspace's data sets — draws one edge on its map.</summary>
/// <remarks>Inserted directly, not through the aggregate's Save — see
/// <see cref="AttachWorkspaceResourceEndpointBase"/> for why.</remarks>
public abstract class CreateWorkspaceRelationshipEndpointBase
    : CrudCreateEndpointBase<CreateWorkspaceRelationshipRequest, WorkspaceMapEdgeDto>
{
    private readonly WorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _access;
    private readonly IDataSetConfigurationProvider _dataSets;

    /// <inheritdoc />
    protected CreateWorkspaceRelationshipEndpointBase(
        ILogger<CreateWorkspaceRelationshipEndpointBase> logger,
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
    protected override string Route => "/workspaces/{Name}/relationships";

    /// <inheritdoc />
    protected override string EndpointSummary => "Declare a relationship between two data sets";

    /// <inheritdoc />
    protected override string EndpointDescription => "Adds one edge to the workspace's map.";

    /// <inheritdoc />
    protected override string GetResourceName(CreateWorkspaceRelationshipRequest request) => request.Name;

    /// <inheritdoc />
    protected override Task<IGenericResult<bool>> CheckExists(CreateWorkspaceRelationshipRequest request, CancellationToken ct)
        // Two data sets may be related more than one way (a lookup join and a rollup join are both
        // real), so there is nothing here to collide with.
        => Task.FromResult(GenericResult<bool>.Success(false));

    /// <summary>Confirms a data set id names a real data set.</summary>
    protected virtual async Task<IGenericResult> ValidateDataSet(string field, Guid dataSetId, CancellationToken ct)
    {
        var found = await _dataSets.Get(dataSetId, ct).ConfigureAwait(false);
        if (found.IsFailure) return found;
        return found.Value is null
            ? GenericResult.Failure(
                WorkspacesResultCodes.ByName("WorkspaceChildNotFound"), Logger,
                ResultDetails.Create("name", field, "kind", "data set", "id", dataSetId.ToString()))
            : GenericResult.Success();
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<WorkspaceMapEdgeDto>> Create(
        CreateWorkspaceRelationshipRequest request, CancellationToken ct)
    {
        if (ReferenceEquals(WorkspaceRelationshipCardinalities.ByName(request.Cardinality), WorkspaceRelationshipCardinalities.NotFound))
        {
            return GenericResult<WorkspaceMapEdgeDto>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceLifecycleValueInvalid"), Logger,
                ResultDetails.Create("name", request.Name, "field", "cardinality", "value", request.Cardinality ?? string.Empty));
        }

        var workspace = await _workspaces.Get(request.Name, ct).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace.ToNewResult<WorkspaceMapEdgeDto>();
        if (workspace.Value is null) return WorkspaceAccessRefusals.Absent<WorkspaceMapEdgeDto>(request.Name, Logger);

        var permitted = await _access.MayWrite(workspace.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted.ToNewResult<WorkspaceMapEdgeDto>();

        var left = await ValidateDataSet("leftDataSetId", request.LeftDataSetId, ct).ConfigureAwait(false);
        if (left.IsFailure) return left.ToNewResult<WorkspaceMapEdgeDto>();

        var right = await ValidateDataSet("rightDataSetId", request.RightDataSetId, ct).ConfigureAwait(false);
        if (right.IsFailure) return right.ToNewResult<WorkspaceMapEdgeDto>();

        var leftField = await WorkspaceFieldOwnershipValidator.Validate(
            _dataSets, Logger, request.Name, "left", request.LeftDataSetId, request.LeftFieldId, ct).ConfigureAwait(false);
        if (leftField.IsFailure) return leftField.ToNewResult<WorkspaceMapEdgeDto>();

        var rightField = await WorkspaceFieldOwnershipValidator.Validate(
            _dataSets, Logger, request.Name, "right", request.RightDataSetId, request.RightFieldId, ct).ConfigureAwait(false);
        if (rightField.IsFailure) return rightField.ToNewResult<WorkspaceMapEdgeDto>();

        var relationship = new WorkspaceRelationshipConfiguration
        {
            Id = Guid.CreateVersion7(),
            // Why the id rather than a derived string: two data sets can be related more than one
            // way, so no deterministic name is unique, and the label is what Name IS for — an
            // unlabelled relationship is the id, not a fabricated title.
            Name = request.Label ?? Guid.CreateVersion7().ToString(),
            LeftDataSetId = request.LeftDataSetId,
            LeftFieldId = request.LeftFieldId,
            RightDataSetId = request.RightDataSetId,
            RightFieldId = request.RightFieldId,
            Cardinality = request.Cardinality,
            // Why stamped here: MsSqlInsertTranslator writes every mapped column explicitly, so the
            // unset CLR default (0001-01-01) would overwrite DEFAULT (sysdatetimeoffset()) rather
            // than deferring to it. (FDW-793)
            CreateDate = DateTimeOffset.UtcNow,
        };

        // The relationship is a child of the workspace aggregate: saving the workspace writes it under the
        // workspace's current row, and the version it replaces keeps the children it had.
        workspace.Value.Relationships.Add(relationship);
        var saved = await _workspaces.Save(
            workspace.Value, workspace.Value.Domain, workspace.Value.Implementation, workspace.Value.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure) return saved.ToNewResult<WorkspaceMapEdgeDto>();

        return GenericResult<WorkspaceMapEdgeDto>.Success(new WorkspaceMapEdgeDto
        {
            Id = relationship.Id.ToString(),
            Source = relationship.LeftDataSetId.ToString(),
            Target = relationship.RightDataSetId.ToString(),
            RelationType = relationship.Cardinality,
            Label = request.Label,
            IsDefined = relationship.LeftFieldId.HasValue && relationship.RightFieldId.HasValue,
            Metadata = BuildEdgeMetadata(relationship),
        });
    }

    private static Dictionary<string, object> BuildEdgeMetadata(WorkspaceRelationshipConfiguration rel)
    {
        var metadata = new Dictionary<string, object>(StringComparer.Ordinal);
        if (rel.LeftFieldId is { } left) metadata["leftFieldId"] = left.ToString();
        if (rel.RightFieldId is { } right) metadata["rightFieldId"] = right.ToString();
        return metadata;
    }
}
