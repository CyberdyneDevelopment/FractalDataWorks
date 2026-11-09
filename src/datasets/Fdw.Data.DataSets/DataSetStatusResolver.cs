using System;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.DataSets;

/// <summary>
/// Picks a data set's status from <see cref="DataSetStatuses"/> (FDW-4488).
/// </summary>
/// <remarks>
/// The options are asked highest rank first and the first whose rule applies wins. The order is the rank and then the name, never registration order, so two hosts
/// with the same options give the same answer, and nothing switches on a status name.
/// </remarks>
public static class DataSetStatusResolver
{
    /// <summary>Resolves the status of one data set.</summary>
    /// <param name="dataSetName">The data set's name, which a failure names.</param>
    /// <param name="input">What the rules read about the data set.</param>
    /// <param name="logger">The logger a failure is written to.</param>
    /// <returns>The status, or the coded failure when no option applies.</returns>
    public static IGenericResult<IDataSetStatus> Resolve(string dataSetName, DataSetStatusInput input, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(logger);
        var status = DataSetStatuses.All()
            .OrderByDescending(option => option.Rank)
            .ThenBy(option => option.Name, StringComparer.Ordinal)
            .FirstOrDefault(option => option.Applies(input));

        return status is null
            ? GenericResult<IDataSetStatus>.Failure(
                DataSetsResultCodes.ByName("NoDataSetStatusApplies"), logger,
                ResultDetails.Create("name", dataSetName, "writerCount", input.Writers.Count))
            : GenericResult<IDataSetStatus>.Success(status);
    }
}
