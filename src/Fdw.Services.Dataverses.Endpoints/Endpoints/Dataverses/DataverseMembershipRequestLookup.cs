using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Dataverses.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Finds one Pending membership request, or the reason it can't be reviewed.</summary>
/// <remarks>
/// Shared by <see cref="ApproveDataverseMembershipRequestEndpointBase"/> and
/// <see cref="DenyDataverseMembershipRequestEndpointBase"/> — both need the same row before either
/// can act on it, and the same two refusals (missing, already reviewed) either way.
/// </remarks>
internal static class DataverseMembershipRequestLookup
{
    /// <summary>Loads the request and confirms it is still Pending.</summary>
    /// <remarks>
    /// Filters IsCurrent=1, not just IsDeleted=0: version-on-write only flips IsCurrent on the row it
    /// retires, never Status, so a retired row's Status column stays frozen at whatever it was before
    /// the retiring save -- often still literally "Pending" forever. Without the IsCurrent filter,
    /// that stale snapshot satisfies this method's own Pending check even after a later version moved
    /// the request to Approved or Declined.
    /// </remarks>
    public static async Task<IGenericResult<DataverseMembershipRequestConfiguration>> FindPending(
        IDataGateway gateway,
        DataverseConfigurationProvider dataverses,
        ILogger logger,
        string dataverseName,
        Guid dataverseImplementationId,
        Guid requestId,
        CancellationToken ct)
    {
        var target = new DataStoreTarget(dataverses.DataStoreName, dataverses.PathName, "DataverseMembershipRequest");
        var existingCommand = new QueryCommand<DataverseMembershipRequestConfiguration>
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = "Id", Operator = FilterOperators.ByName("Equal"), Value = requestId },
                        new FilterCondition { PropertyName = "DataverseImplementationId", Operator = FilterOperators.ByName("Equal"), Value = dataverseImplementationId },
                        new FilterCondition { PropertyName = "IsCurrent", Operator = FilterOperators.ByName("Equal"), Value = true },
                        new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                    ]
                }
            }
        };

        var existing = await gateway.Execute<IEnumerable<DataverseMembershipRequestConfiguration>>(existingCommand, target, ct)
            .ConfigureAwait(false);
        if (existing.IsFailure) return existing.ToNewResult<DataverseMembershipRequestConfiguration>();

        var found = existing.Value?.FirstOrDefault();
        if (found is null)
        {
            return GenericResult<DataverseMembershipRequestConfiguration>.Failure(
                DataversesResultCodes.ByName("DataverseChildNotFound"), logger,
                ResultDetails.Create("name", dataverseName, "kind", "membership request", "id", requestId.ToString()));
        }

        if (!string.Equals(found.Status, "Pending", StringComparison.Ordinal))
        {
            return GenericResult<DataverseMembershipRequestConfiguration>.Failure(
                DataversesResultCodes.ByName("DataverseLifecycleValueInvalid"), logger,
                ResultDetails.Create("name", dataverseName, "field", "status", "value", found.Status));
        }

        return GenericResult<DataverseMembershipRequestConfiguration>.Success(found);
    }
}
