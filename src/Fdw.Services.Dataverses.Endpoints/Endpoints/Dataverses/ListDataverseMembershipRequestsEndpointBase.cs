using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Lists a dataverse's membership requests.</summary>
/// <remarks>
/// DataverseMembershipRequestConfiguration is not composed onto the aggregate (unlike
/// Members/Resources/Relationships), so this reads directly through the gateway, the same shape as
/// the write-side endpoints in this file group.
/// </remarks>
public abstract class ListDataverseMembershipRequestsEndpointBase
    : CrudListEndpointBase<DataverseNameRequest, DataverseMembershipRequestDto>
{
    private readonly DataverseConfigurationProvider _dataverses;
    private readonly IDataGatewayProvider _dataGateways;

    private IDataGateway Gateway => _dataGateways.ByName("Main");

    /// <inheritdoc />
    protected ListDataverseMembershipRequestsEndpointBase(
        ILogger<ListDataverseMembershipRequestsEndpointBase> logger,
        DataverseConfigurationProvider dataverses,
        IDataGatewayProvider dataGateways) : base(logger)
    {
        _dataverses = dataverses;
        _dataGateways = dataGateways;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "dataverses";

    /// <inheritdoc />
    protected override string Route => "/dataverses/{Name}/membership-requests";

    /// <inheritdoc />
    protected override string EndpointSummary => "List a dataverse's membership requests";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns every request to join the dataverse, reviewed or not.";

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<DataverseMembershipRequestDto>>> LoadItems(
        DataverseNameRequest request, CancellationToken ct)
    {
        var dataverse = await _dataverses.Get(request.Name, ct).ConfigureAwait(false);
        if (dataverse.IsFailure) return dataverse.ToNewResult<List<DataverseMembershipRequestDto>>();
        if (dataverse.Value is null) return GenericResult<List<DataverseMembershipRequestDto>>.Success([]);

        var target = new DataStoreTarget(_dataverses.DataStoreName, _dataverses.PathName, "DataverseMembershipRequest");
        var command = new QueryCommand<DataverseMembershipRequestConfiguration>
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = "DataverseImplementationId", Operator = FilterOperators.ByName("Equal"), Value = dataverse.Value.Id },
                        new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                    ]
                }
            }
        };

        var result = await Gateway.Execute<IEnumerable<DataverseMembershipRequestConfiguration>>(command, target, ct)
            .ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<List<DataverseMembershipRequestDto>>();

        var requests = (result.Value ?? [])
            .Select(DataverseMembershipRequestDtos.From)
            .ToList();

        return GenericResult<List<DataverseMembershipRequestDto>>.Success(requests);
    }
}
