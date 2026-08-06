using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions.FieldAccess;
using Fdw.Commands.Data.Abstractions.JoinExecutors;

namespace Fdw.Commands.Data.Joins;

/// <summary>
/// Executes a left outer join between two record sets.
/// Returns all rows from the left source, with matching rows from the right (or default).
/// </summary>
[TypeOption(typeof(JoinExecutors), "Left")]
public sealed class LeftJoinExecutor : JoinExecutorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LeftJoinExecutor"/> class.
    /// </summary>
    public LeftJoinExecutor() : base(2, "Left") { }

    /// <inheritdoc />
    public override IEnumerable<TResult> Execute<TLeft, TRight, TResult>(
        IEnumerable<TLeft> leftRecords,
        IEnumerable<TRight> rightRecords,
        IFieldValueExtractor fieldExtractor,
        (string LeftField, string RightField) condition,
        Func<TLeft, TRight, TResult> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(leftRecords);
        ArgumentNullException.ThrowIfNull(rightRecords);
        ArgumentNullException.ThrowIfNull(fieldExtractor);
        ArgumentNullException.ThrowIfNull(resultSelector);

        // Build hash table for right side (O(m))
        var rightLookup = rightRecords
            .ToLookup(r => fieldExtractor.GetValue(r, condition.RightField));

        // LEFT OUTER JOIN - all left rows, matching right rows (or default)
        foreach (var leftRecord in leftRecords)
        {
            var leftValue = fieldExtractor.GetValue(leftRecord, condition.LeftField);
            var matchingRights = rightLookup[leftValue].ToList();

            if (matchingRights.Count > 0)
            {
                foreach (var rightRecord in matchingRights)
                {
                    yield return resultSelector(leftRecord, rightRecord);
                }
            }
            else
            {
                yield return resultSelector(leftRecord, default!);
            }
        }
    }
}
