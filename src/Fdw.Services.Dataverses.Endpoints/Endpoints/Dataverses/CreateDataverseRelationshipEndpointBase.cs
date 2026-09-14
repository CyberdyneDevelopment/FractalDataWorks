using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Dataverses.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Declares a relationship between two of a dataverse's data sets — draws one edge on its map.</summary>
/// <remarks>Inserted directly, not through the aggregate's Save — see
/// <see cref="AttachDataverseResourceEndpointBase"/> for why.</remarks>
public abstract class CreateDataverseRelationshipEndpointBase
    : CrudCreateEndpointBase<CreateDataverseRelationshipRequest, DataverseMapEdgeDto>
{
    private readonly DataverseConfigurationProvider _dataverses;
    private readonly IDataverseAccessPolicy _access;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly IDataSetConfigurationProvider _dataSets;

    private IDataGateway Gateway => _dataGateways.ByName("Main");

    /// <inheritdoc />
    protected CreateDataverseRelationshipEndpointBase(
        ILogger<CreateDataverseRelationshipEndpointBase> logger,
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
    protected override string Route => "/dataverses/{Name}/relationships";

    /// <inheritdoc />
    protected override string EndpointSummary => "Declare a relationship between two data sets";

    /// <inheritdoc />
    protected override string EndpointDescription => "Adds one edge to the dataverse's map.";

    /// <inheritdoc />
    protected override string GetResourceName(CreateDataverseRelationshipRequest request) => request.Name;

    /// <inheritdoc />
    protected override Task<IGenericResult<bool>> CheckExists(CreateDataverseRelationshipRequest request, CancellationToken ct)
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
                DataversesResultCodes.ByName("DataverseChildNotFound"), Logger,
                ResultDetails.Create("name", field, "kind", "data set", "id", dataSetId.ToString()))
            : GenericResult.Success();
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataverseMapEdgeDto>> Create(
        CreateDataverseRelationshipRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Cardinality))
        {
            return GenericResult<DataverseMapEdgeDto>.Failure(
                DataversesResultCodes.ByName("DataverseLifecycleValueInvalid"), Logger,
                ResultDetails.Create("name", request.Name, "field", "cardinality", "value", request.Cardinality ?? string.Empty));
        }

        var dataverse = await _dataverses.Get(request.Name, ct).ConfigureAwait(false);
        if (dataverse.IsFailure) return dataverse.ToNewResult<DataverseMapEdgeDto>();
        if (dataverse.Value is null)
        {
            return GenericResult<DataverseMapEdgeDto>.Failure(
                DataversesResultCodes.ByName("DataverseLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", request.Name));
        }

        var permitted = await _access.MayWrite(dataverse.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted.ToNewResult<DataverseMapEdgeDto>();

        var left = await ValidateDataSet("leftDataSetId", request.LeftDataSetId, ct).ConfigureAwait(false);
        if (left.IsFailure) return left.ToNewResult<DataverseMapEdgeDto>();

        var right = await ValidateDataSet("rightDataSetId", request.RightDataSetId, ct).ConfigureAwait(false);
        if (right.IsFailure) return right.ToNewResult<DataverseMapEdgeDto>();

        var relationship = new DataverseRelationshipConfiguration
        {
            Id = Guid.CreateVersion7(),
            // Why the id rather than a derived string: two data sets can be related more than one
            // way, so no deterministic name is unique, and the label is what Name IS for — an
            // unlabelled relationship is the id, not a fabricated title.
            Name = request.Label ?? Guid.CreateVersion7().ToString(),
            DataverseImplementationId = dataverse.Value.Id,
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

        // ConfigurationSaveCommand, not InsertCommand -- see AttachDataverseResourceEndpointBase
        // for why: DataverseImplementationRowId is a physical FK with no matching C# property, and
        // only MsSqlConfigurationSaveTranslator resolves it via subquery.
        var target = new DataStoreTarget(_dataverses.DataStoreName, _dataverses.PathName, "DataverseRelationship");
        var insertResult = await Gateway.Execute<int>(
            new ConfigurationSaveCommand<DataverseRelationshipConfiguration>(relationship), target, ct)
            .ConfigureAwait(false);
        if (insertResult.IsFailure) return insertResult.ToNewResult<DataverseMapEdgeDto>();

        return GenericResult<DataverseMapEdgeDto>.Success(new DataverseMapEdgeDto
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

    private static Dictionary<string, object> BuildEdgeMetadata(DataverseRelationshipConfiguration rel)
    {
        var metadata = new Dictionary<string, object>(StringComparer.Ordinal);
        if (rel.LeftFieldId is { } left) metadata["leftFieldId"] = left.ToString();
        if (rel.RightFieldId is { } right) metadata["rightFieldId"] = right.ToString();
        return metadata;
    }
}
