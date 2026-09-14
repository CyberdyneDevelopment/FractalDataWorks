using System;
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
using Fdw.Services.Dataverses.Abstractions;
using Fdw.Services.Dataverses.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Attaches a resource to a dataverse — puts one node on its map.</summary>
/// <remarks>
/// Inserted directly through the gateway rather than by loading the dataverse aggregate, mutating
/// its composed Resources list and calling the domain provider's Save: that Save cascades every
/// item currently in the list as a fresh INSERT (ImplementationProviderBase.SaveOneChild has no
/// update/diff path), so re-saving an aggregate that already has resources attached would duplicate
/// every one of them. One row in, one INSERT — the same shape SaveSourceMappingsEndpointBase uses
/// for FieldMapping, and for the same reason.
/// </remarks>
public abstract class AttachDataverseResourceEndpointBase
    : CrudCreateEndpointBase<AttachDataverseResourceRequest, DataverseMapNodeDto>
{
    private readonly DataverseConfigurationProvider _dataverses;
    private readonly IDataverseAccessPolicy _access;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly IAuthenticationContextAccessor _authContext;

    // Why resolved here rather than injected: the gateway is scoped and this is not, so holding one
    // would be a captive dependency. The provider is asked when a call is actually being made.
    private IDataGateway Gateway => _dataGateways.ByName("Main");

    /// <inheritdoc />
    protected AttachDataverseResourceEndpointBase(
        ILogger<AttachDataverseResourceEndpointBase> logger,
        DataverseConfigurationProvider dataverses,
        IDataverseAccessPolicy access,
        IDataGatewayProvider dataGateways,
        IDataSetConfigurationProvider dataSets,
        IAuthenticationContextAccessor authContext) : base(logger)
    {
        _dataverses = dataverses;
        _access = access;
        _dataGateways = dataGateways;
        _dataSets = dataSets;
        _authContext = authContext;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "dataverses";

    /// <inheritdoc />
    protected override string Route => "/dataverses/{Name}/resources";

    /// <inheritdoc />
    protected override string EndpointSummary => "Attach a resource to a dataverse";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Adds one node to the dataverse's map. Returns the created attachment.";

    /// <inheritdoc />
    protected override string GetResourceName(AttachDataverseResourceRequest request) => request.Name;

    /// <summary>Resolves a human label for the attached resource, for the kinds that cost one lookup.</summary>
    /// <remarks>
    /// Only DataSet is wired today. An unresolvable kind refuses rather than falling back to the raw
    /// resource id as a label — a resource genuinely has no name yet for kinds this has not been
    /// extended to, and inventing one would read as a real label rather than as the gap it is.
    /// </remarks>
    protected virtual async Task<IGenericResult<string>> ResolveResourceLabel(
        string resourceType, Guid resourceId, CancellationToken ct)
    {
        if (string.Equals(resourceType, "DataSet", StringComparison.Ordinal))
        {
            var dataSet = await _dataSets.Get(resourceId, ct).ConfigureAwait(false);
            if (dataSet.IsFailure) return dataSet.ToNewResult<string>();
            return dataSet.Value is { Name.Length: > 0 } found
                ? GenericResult<string>.Success(found.Name)
                : GenericResult<string>.Failure(
                    DataversesResultCodes.ByName("DataverseChildNotFound"), Logger,
                    ResultDetails.Create("name", resourceType, "kind", "resource", "id", resourceId.ToString()));
        }

        return GenericResult<string>.Failure(
            DataversesResultCodes.ByName("DataverseLifecycleValueInvalid"), Logger,
            ResultDetails.Create("name", resourceType, "field", "resourceType",
                "value", $"{resourceType} (no label resolver wired for this kind yet)"));
    }

    /// <inheritdoc />
    protected override Task<IGenericResult<bool>> CheckExists(AttachDataverseResourceRequest request, CancellationToken ct)
        // A dataverse may attach the same data set twice under different relationships (a
        // relationship it Owns and one it merely Uses), so there is nothing here to collide with.
        => Task.FromResult(GenericResult<bool>.Success(false));

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataverseMapNodeDto>> Create(
        AttachDataverseResourceRequest request, CancellationToken ct)
    {
        if (DataverseResourceKinds.ByName(request.ResourceType) is null)
        {
            return GenericResult<DataverseMapNodeDto>.Failure(
                DataversesResultCodes.ByName("DataverseLifecycleValueInvalid"), Logger,
                ResultDetails.Create("name", request.Name, "field", "resourceType", "value", request.ResourceType));
        }

        if (string.IsNullOrWhiteSpace(request.Relationship))
        {
            return GenericResult<DataverseMapNodeDto>.Failure(
                DataversesResultCodes.ByName("DataverseLifecycleValueInvalid"), Logger,
                ResultDetails.Create("name", request.Name, "field", "relationship", "value", request.Relationship ?? string.Empty));
        }

        var dataverse = await _dataverses.Get(request.Name, ct).ConfigureAwait(false);
        if (dataverse.IsFailure) return dataverse.ToNewResult<DataverseMapNodeDto>();
        if (dataverse.Value is null)
        {
            return GenericResult<DataverseMapNodeDto>.Failure(
                DataversesResultCodes.ByName("DataverseLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", request.Name));
        }

        var permitted = await _access.MayWrite(dataverse.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted.ToNewResult<DataverseMapNodeDto>();

        var label = await ResolveResourceLabel(request.ResourceType, request.ResourceId, ct).ConfigureAwait(false);
        if (label.IsFailure) return label.ToNewResult<DataverseMapNodeDto>();

        if (_authContext.Current is not { } caller || !Guid.TryParse(caller.UserId, out var addedByUserId))
        {
            return GenericResult<DataverseMapNodeDto>.Failure(
                DataversesResultCodes.ByName("DataverseOwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "the caller could not be resolved"));
        }

        var resource = new DataverseResourceConfiguration
        {
            Id = Guid.CreateVersion7(),
            // The column is NOT NULL but carries no identity of its own beyond the resource it
            // names — the attachment IS the row, so its Name is the resource's own label.
            Name = label.Value!,
            DataverseImplementationId = dataverse.Value.Id,
            ResourceType = request.ResourceType,
            ResourceId = request.ResourceId,
            Relationship = request.Relationship,
            AddedByUserId = addedByUserId,
            // Why stamped here: MsSqlInsertTranslator writes every mapped column explicitly, so the
            // unset CLR default (0001-01-01) would overwrite DEFAULT (sysdatetimeoffset()) rather
            // than deferring to it. (FDW-793)
            CreateDate = DateTimeOffset.UtcNow,
        };

        // ConfigurationSaveCommand, not InsertCommand: DataverseImplementationRowId is a physical
        // FK with no matching C# property (only the logical DataverseImplementationId is on the
        // POCO), and only MsSqlConfigurationSaveTranslator resolves that via a subquery on the
        // logical Id -- a plain InsertCommand sends it as NULL and the NOT NULL constraint refuses
        // the row. Version-on-write is harmless here: there is no existing current row to retire.
        var target = new DataStoreTarget(_dataverses.DataStoreName, _dataverses.PathName, "DataverseResource");
        var insertResult = await Gateway.Execute<int>(
            new ConfigurationSaveCommand<DataverseResourceConfiguration>(resource), target, ct)
            .ConfigureAwait(false);
        if (insertResult.IsFailure) return insertResult.ToNewResult<DataverseMapNodeDto>();

        return GenericResult<DataverseMapNodeDto>.Success(new DataverseMapNodeDto
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
