using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Names the data stores the host declared in configurationSchema.json: the platform's own, which no data store list or
/// lookup offers (FDW-1068).
/// </summary>
/// <remarks>
/// One definition, read off the stores the configuration gateway built (<c>IDataStore.DeclaredByHost</c>), not from a
/// list of names, so the list and the lookup leave out the same stores.
/// </remarks>
internal static class HostDeclaredDataStores
{
    /// <summary>Reads the names of the stores the host declared.</summary>
    /// <param name="configurationGateways">Holds the stores the host declared.</param>
    /// <returns>The names, compared without regard to case; the failure when the gateway could not be read.</returns>
    internal static IGenericResult<HashSet<string>> Names(IConfigurationGatewayProvider configurationGateways)
    {
        var declared = configurationGateways.Get(DataStoreTypes.ConfigurationConnection);
        return declared.IsFailure
            ? declared.ToNewResult<HashSet<string>>()
            : GenericResult<HashSet<string>>.Success(new HashSet<string>(
                declared.Value!.DataStores.Where(store => store.DeclaredByHost).Select(store => store.Name),
                StringComparer.OrdinalIgnoreCase));
    }
}
