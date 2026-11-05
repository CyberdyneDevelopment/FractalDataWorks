using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// Builds the <see cref="DataStoreDiscoveryOptions"/> of a discovery from the choices its request states (FDW-3823).
/// </summary>
/// <remarks>
/// Mike, 2026-11-04: "why in the fuck would you make a selection that is a default". Which schemas are left out and whether views,
/// indexes, foreign keys and descriptions are read are the caller's choices, so each is required on the request and a request that
/// leaves one out is refused, naming it. An empty excluded list is a stated choice (exclude none); only a missing one is refused.
/// </remarks>
public static class DataStoreDiscoveryChoices
{
    /// <summary>Builds the options, or the failure naming the first choice the request leaves out.</summary>
    /// <param name="excludedSchemas">The schemas to leave out.</param>
    /// <param name="includeOnlySchemas">The only schemas to read; null reads every schema not excluded.</param>
    /// <param name="discoverViews">Whether views are read.</param>
    /// <param name="discoverIndexes">Whether indexes are read.</param>
    /// <param name="discoverForeignKeys">Whether foreign keys are read.</param>
    /// <param name="discoverDescriptions">Whether descriptions are read.</param>
    /// <param name="owner">What is discovered, named in a failure.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The options, or the refusal.</returns>
    public static IGenericResult<DataStoreDiscoveryOptions> FromRequest(
        IEnumerable<string>? excludedSchemas,
        IEnumerable<string>? includeOnlySchemas,
        bool? discoverViews,
        bool? discoverIndexes,
        bool? discoverForeignKeys,
        bool? discoverDescriptions,
        string owner,
        ILogger logger)
    {
        var missing = excludedSchemas is null ? "ExcludedSchemas"
            : discoverViews is null ? "DiscoverViews"
            : discoverIndexes is null ? "DiscoverIndexes"
            : discoverForeignKeys is null ? "DiscoverForeignKeys"
            : discoverDescriptions is null ? "DiscoverDescriptions"
            : null;
        if (missing is not null)
            return GenericResult<DataStoreDiscoveryOptions>.Failure(SchemaDiscoveryLog.DiscoveryChoiceNotStated(logger, owner, missing));

        return GenericResult<DataStoreDiscoveryOptions>.Success(new DataStoreDiscoveryOptions
        {
            ExcludedSchemas = [.. excludedSchemas!],
            IncludeOnlySchemas = includeOnlySchemas?.ToList(),
            DiscoverViews = discoverViews!.Value,
            DiscoverIndexes = discoverIndexes!.Value,
            DiscoverForeignKeys = discoverForeignKeys!.Value,
            DiscoverDescriptions = discoverDescriptions!.Value,
        });
    }
}
