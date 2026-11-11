using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads.Builders;

/// <summary>
/// Checks a set-based load command's rows carry a value for every key column (FDW-4953, FDW-4954): the batch is matched on its
/// keys in one statement, and a null key matches nothing.
/// </summary>
internal static class LoadKeyFilters
{
    /// <summary>Checks every row has a value for every key column: a null key matches nothing.</summary>
    /// <param name="owner">What the load is, for the refusal message.</param>
    /// <param name="keys">The key columns.</param>
    /// <param name="rows">The rows, which carry every key column.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>Success, or a failure naming the first key column a row has no value for.</returns>
    public static IGenericResult KeysPresent(
        string owner,
        IReadOnlyList<string> keys,
        IReadOnlyList<IDictionary<string, object?>> rows,
        ILogger logger)
    {
        foreach (var row in rows)
        {
            var missing = keys.FirstOrDefault(key => !LoadRows.TryFind(row, key, out var value) || value is null);
            if (missing is not null)
                return GenericResult.Failure(PipelineRunLog.KeyValueMissing(logger, owner, missing));
        }

        return GenericResult.Success();
    }
}
