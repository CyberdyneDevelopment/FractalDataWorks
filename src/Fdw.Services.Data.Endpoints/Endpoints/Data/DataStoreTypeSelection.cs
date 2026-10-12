using System;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Chooses the data store type of a new data store from what its connection's kind offers.
/// </summary>
public static class DataStoreTypeSelection
{
    /// <summary>
    /// Gets the data store type the new store has: the one the request names, or the kind's first when it names none.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="connectionImplementation">The connection's implementation name, which names its kind.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>
    /// The type name, or a failure naming the types the kind does offer. A kind that is not registered keeps the
    /// earlier behaviour: the store takes the connection's implementation name.
    /// </returns>
    public static IGenericResult<string> Select(CreateDataStoreRequest request, string connectionImplementation, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(logger);

        var kind = ConnectionTypes.ByName(connectionImplementation);
        if (kind == ConnectionTypes.NotFound)
        {
            return GenericResult<string>.Success(connectionImplementation);
        }

        var offered = kind.SupportedDataStoreTypes;
        var named = string.IsNullOrWhiteSpace(request.StoreType)
            ? (offered.Count > 0 ? offered[0] : null)
            : offered.FirstOrDefault(n => string.Equals(n, request.StoreType, StringComparison.OrdinalIgnoreCase));
        return named is not null
            ? GenericResult<string>.Success(named)
            : GenericResult<string>.Failure(DataStoreEndpointLog.StoreTypeNotAvailable(
                logger, request.Name, request.StoreType ?? "(default)", connectionImplementation, string.Join(", ", offered)));
    }
}
