using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// Asks a data set's own kind whether a read route can answer a paged or a grouped query from it, so every route that reads
/// a data set refuses the same sets for the same reasons (FDW-1054).
/// </summary>
/// <remarks>
/// Why one gate: a route that reads a data set's first source directly, or pages it without asking, answers a Federated,
/// Compound or aggregating set with the wrong rows and no sign anything is wrong. The kind decides, through
/// <see cref="IDataSetType.PagesInStore"/> and <see cref="IDataSetType.AggregatesInStore"/>, and a route never names kinds.
/// A kind that learns to page or group starts working on every route at once.
/// </remarks>
public static class DataSetReadGate
{
    /// <summary>Refuses a data set whose kind, or whose aggregation, would page wrongly.</summary>
    /// <remarks>
    /// Asked of the data set's own kind through <see cref="IDataSetType.PagesInStore"/> rather than by naming kinds here.
    /// An unknown kind resolves to NotFound, which does not page, so it is refused too.
    /// </remarks>
    /// <param name="dataSet">The data set the route is asked to read.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>Success, or the refusal the caller is answered with.</returns>
    public static IGenericResult Pageable(IDataSetImplementationConfiguration dataSet, ILogger logger)
    {
        if (!DataSetTypes.ByName(dataSet.Implementation).PagesInStore)
            return GenericResult.Failure(
                DataServiceResultCodes.ByName("DataSetNotPageable"), logger,
                ResultDetails.Create("dataSet", dataSet.Name,
                    "reason", $"kind '{dataSet.Implementation}' does not page in the store"));

        return dataSet.AggregatesOrNone().Count > 0
            ? GenericResult.Failure(
                DataServiceResultCodes.ByName("DataSetNotPageable"), logger,
                ResultDetails.Create("dataSet", dataSet.Name,
                    "reason", "it aggregates, and aggregated queries take no paging"))
            : GenericResult.Success();
    }

    /// <summary>Refuses a data set whose kind builds its rows from several reads, or that already aggregates, so a grouping has no one query to join.</summary>
    /// <remarks>
    /// Asked of the data set's own kind through <see cref="IDataSetType.AggregatesInStore"/>, the same way paging is.
    /// </remarks>
    /// <param name="dataSet">The data set the route is asked to group.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>Success, or the refusal the caller is answered with.</returns>
    public static IGenericResult Groupable(IDataSetImplementationConfiguration dataSet, ILogger logger)
    {
        if (!DataSetTypes.ByName(dataSet.Implementation).AggregatesInStore)
            return GenericResult.Failure(
                DataServiceResultCodes.ByName("DataSetNotGroupable"), logger,
                ResultDetails.Create("dataSet", dataSet.Name,
                    "reason", $"kind '{dataSet.Implementation}' does not run a grouped query in the store"));

        return dataSet.AggregatesOrNone().Count > 0
            ? GenericResult.Failure(
                DataServiceResultCodes.ByName("DataSetNotGroupable"), logger,
                ResultDetails.Create("dataSet", dataSet.Name, "reason", "it already defines aggregates of its own"))
            : GenericResult.Success();
    }
}
