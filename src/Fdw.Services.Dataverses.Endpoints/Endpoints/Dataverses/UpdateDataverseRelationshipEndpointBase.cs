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
using Fdw.Services.Dataverses.Abstractions;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Sets or changes a relationship's join fields — the one thing create can leave unresolved.</summary>
/// <remarks>
/// A relationship can be declared before anyone has picked which columns carry it (both
/// LeftFieldId/RightFieldId are nullable on the row), and until FDW-794's addendum there was no way
/// to name them afterward. Version-on-write via <see cref="ConfigurationSaveCommand{T}"/>, same as
/// create and detach on this domain, carrying every unrelated column forward from the row already
/// loaded by <see cref="FindForUpdate"/> rather than reconstructing it from the request.
/// </remarks>
public abstract class UpdateDataverseRelationshipEndpointBase
    : CrudUpdateEndpointBase<UpdateDataverseRelationshipRequest, DataverseMapEdgeDto>
{
    private readonly DataverseConfigurationProvider _dataverses;
    private readonly IDataverseAccessPolicy _access;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly IDataSetConfigurationProvider _dataSets;

    private IDataGateway Gateway => _dataGateways.ByName("Main");

    // Set by FindForUpdate, read by Update -- both run within the same request via
    // CrudUpdateEndpointBase.HandleAsync, in that order, on a per-request endpoint instance.
    private DataverseRelationshipConfiguration? _existing;

    /// <inheritdoc />
    protected UpdateDataverseRelationshipEndpointBase(
        ILogger<UpdateDataverseRelationshipEndpointBase> logger,
        DataverseConfigurationProvider dataverses,
        IDataverseAccessPolicy access,
        IDataGatewayProvider dataGateways,
        IDataSetConfigurationProvider dataSets) : base(logger)
    {
        _dataverses = dataverses;
        _access = access;
        _dataGateways = dataGateways;
        _dataSets = dataSets;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "dataverses";

    /// <inheritdoc />
    protected override string Route => "/dataverses/{Name}/relationships/{RelationshipId}";

    /// <inheritdoc />
    protected override string EndpointSummary => "Set a relationship's join fields";

    /// <inheritdoc />
    protected override string EndpointDescription => "Changes which fields two related data sets join on.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(UpdateDataverseRelationshipRequest request)
        => request.RelationshipId.ToString();

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataverseMapEdgeDto?>> FindForUpdate(
        UpdateDataverseRelationshipRequest request, CancellationToken ct)
    {
        var dataverse = await _dataverses.Get(request.Name, ct).ConfigureAwait(false);
        if (dataverse.IsFailure) return dataverse.ToNewResult<DataverseMapEdgeDto?>();
        if (dataverse.Value is null) return GenericResult<DataverseMapEdgeDto?>.Success(null);

        var permitted = await _access.MayWrite(dataverse.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted.ToNewResult<DataverseMapEdgeDto?>();

        var target = new DataStoreTarget(_dataverses.DataStoreName, _dataverses.PathName, "DataverseRelationship");
        var existingCommand = new QueryCommand<DataverseRelationshipConfiguration>
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = "Id", Operator = FilterOperators.ByName("Equal"), Value = request.RelationshipId },
                        new FilterCondition { PropertyName = "DataverseImplementationId", Operator = FilterOperators.ByName("Equal"), Value = dataverse.Value.Id },
                        new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                    ]
                }
            }
        };

        var existing = await Gateway.Execute<IEnumerable<DataverseRelationshipConfiguration>>(existingCommand, target, ct)
            .ConfigureAwait(false);
        if (existing.IsFailure) return existing.ToNewResult<DataverseMapEdgeDto?>();

        _existing = existing.Value?.FirstOrDefault();
        return GenericResult<DataverseMapEdgeDto?>.Success(_existing is null ? null : ToDto(_existing));
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataverseMapEdgeDto>> Update(
        UpdateDataverseRelationshipRequest request, DataverseMapEdgeDto existing, CancellationToken ct)
    {
        // FindForUpdate always runs before Update in CrudUpdateEndpointBase.HandleAsync, and a null
        // _existing there already short-circuits to 404 before Update is ever called.
        var row = _existing!;

        var leftField = await DataverseFieldOwnershipValidator.Validate(
            _dataSets, Logger, request.Name, "left", row.LeftDataSetId, request.LeftFieldId, ct).ConfigureAwait(false);
        if (leftField.IsFailure) return leftField.ToNewResult<DataverseMapEdgeDto>();

        var rightField = await DataverseFieldOwnershipValidator.Validate(
            _dataSets, Logger, request.Name, "right", row.RightDataSetId, request.RightFieldId, ct).ConfigureAwait(false);
        if (rightField.IsFailure) return rightField.ToNewResult<DataverseMapEdgeDto>();

        row.LeftFieldId = request.LeftFieldId;
        row.RightFieldId = request.RightFieldId;
        // Why stamped here: MsSqlConfigurationSaveTranslator's INSERT half writes every mapped
        // column explicitly, same as the create/attach sites (FDW-793) -- CreateDate is left as the
        // value already on `row` (the original creation date, not this version's), and only
        // ModifyDate moves.
        row.ModifyDate = DateTimeOffset.UtcNow;

        var target = new DataStoreTarget(_dataverses.DataStoreName, _dataverses.PathName, "DataverseRelationship");
        var saveResult = await Gateway.Execute<int>(
            new ConfigurationSaveCommand<DataverseRelationshipConfiguration>(row), target, ct)
            .ConfigureAwait(false);
        if (saveResult.IsFailure) return saveResult.ToNewResult<DataverseMapEdgeDto>();

        return GenericResult<DataverseMapEdgeDto>.Success(ToDto(row));
    }

    private static DataverseMapEdgeDto ToDto(DataverseRelationshipConfiguration rel) => new()
    {
        Id = rel.Id.ToString(),
        Source = rel.LeftDataSetId.ToString(),
        Target = rel.RightDataSetId.ToString(),
        RelationType = rel.Cardinality,
        // Matches GetDataverseMapEndpointBase's Label = rel.Name -- the row's own name, not the
        // create request's optional label, since an unlabelled relationship's Name is its own id
        // string and that IS what the map shows for it.
        Label = rel.Name,
        IsDefined = rel.LeftFieldId.HasValue && rel.RightFieldId.HasValue,
        Metadata = BuildEdgeMetadata(rel),
    };

    private static Dictionary<string, object> BuildEdgeMetadata(DataverseRelationshipConfiguration rel)
    {
        var metadata = new Dictionary<string, object>(StringComparer.Ordinal);
        if (rel.LeftFieldId is { } left) metadata["leftFieldId"] = left.ToString();
        if (rel.RightFieldId is { } right) metadata["rightFieldId"] = right.ToString();
        return metadata;
    }
}
