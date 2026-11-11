using System;
using System.Collections.Generic;
using System.Globalization;
using Fdw.Commands.Data.Abstractions.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// What a set-based update or delete that reports its unmatched keys answers (FDW-4988): the rows it wrote, the exact number of its distinct keys that matched
/// no row in the target, and up to the stated number of those keys.
/// </summary>
/// <param name="RowsAffected">The rows the write affected.</param>
/// <param name="UnmatchedKeys">How many of the command's distinct keys had no row in the target, counted in the same batch as the write.</param>
/// <param name="UnmatchedSample">Up to the command's sample size of those keys, each as its key columns and values.</param>
public sealed record BulkWriteAnswer(long RowsAffected, long UnmatchedKeys, IReadOnlyList<IReadOnlyDictionary<string, object?>> UnmatchedSample)
{
    /// <summary>The column of the first result set that holds the rows the write affected.</summary>
    public const string AffectedColumn = "fdw_affected";

    /// <summary>The column of the first result set that holds the number of keys that matched no row.</summary>
    public const string UnmatchedColumn = "fdw_unmatched";

    /// <summary>
    /// Reads the answer from the result sets a connection read apart: the first holds one row with <see cref="AffectedColumn"/> and
    /// <see cref="UnmatchedColumn"/>, the second the unmatched keys. Anything else is refused by name, never read as zero.
    /// </summary>
    /// <param name="commandType">The command that answered, for messages.</param>
    /// <param name="sets">The result sets, in the order the batch selected them.</param>
    /// <param name="sampleLimit">The most keys the command asked for.</param>
    /// <returns>The answer, or why the sets cannot be one.</returns>
    public static IGenericResult<BulkWriteAnswer> From(string commandType, IReadOnlyList<IReadOnlyList<IReadOnlyDictionary<string, object?>>> sets, int sampleLimit)
    {
        var logger = NullLogger.Instance;
        if (sets.Count != 2)
            return GenericResult<BulkWriteAnswer>.Failure(BulkWriteAnswerLog.SetsNotTwo(logger, commandType, sets.Count));
        if (sets[0].Count != 1)
            return GenericResult<BulkWriteAnswer>.Failure(BulkWriteAnswerLog.SummaryNotOneRow(logger, commandType, sets[0].Count));

        var summary = sets[0][0];
        if (!TryCount(summary, AffectedColumn, out var affected))
            return GenericResult<BulkWriteAnswer>.Failure(BulkWriteAnswerLog.SummaryColumnUnreadable(logger, commandType, AffectedColumn));
        if (!TryCount(summary, UnmatchedColumn, out var unmatched))
            return GenericResult<BulkWriteAnswer>.Failure(BulkWriteAnswerLog.SummaryColumnUnreadable(logger, commandType, UnmatchedColumn));
        if (unmatched < 0 || affected < 0)
            return GenericResult<BulkWriteAnswer>.Failure(BulkWriteAnswerLog.CountNegative(logger, commandType, affected, unmatched));

        var keys = sets[1];
        if (keys.Count > sampleLimit)
            return GenericResult<BulkWriteAnswer>.Failure(BulkWriteAnswerLog.SampleOverLimit(logger, commandType, keys.Count, sampleLimit));
        return keys.Count > unmatched
            ? GenericResult<BulkWriteAnswer>.Failure(BulkWriteAnswerLog.SampleOverCount(logger, commandType, keys.Count, unmatched))
            : GenericResult<BulkWriteAnswer>.Success(new BulkWriteAnswer(affected, unmatched, keys));
    }

    private static bool TryCount(IReadOnlyDictionary<string, object?> row, string column, out long count)
    {
        count = 0;
        if (!row.TryGetValue(column, out var value) || value is null)
            return false;

        switch (value)
        {
            case long whole:
                count = whole;
                return true;
            case int small:
                count = small;
                return true;
            case decimal exact when exact == decimal.Truncate(exact):
                count = decimal.ToInt64(exact);
                return true;
            default:
                return value is IConvertible convertible
                    && long.TryParse(convertible.ToString(CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out count);
        }
    }
}
