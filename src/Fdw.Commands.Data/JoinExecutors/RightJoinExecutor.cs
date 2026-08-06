using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions.FieldAccess;
using Fdw.Commands.Data.Abstractions.JoinExecutors;

namespace Fdw.Commands.Data.Joins;

/// <summary>
/// Executes a right outer join between two record sets.
/// Returns all rows from the right source, with matching rows from the left (or default).
/// </summary>
[TypeOption(typeof(JoinExecutors), "Right")]
public sealed class RightJoinExecutor : JoinExecutorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RightJoinExecutor"/> class.
    /// </summary>
    public RightJoinExecutor() : base(3, "Right") { }

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

        // Build hash table for left side
        var leftLookup = leftRecords
            .ToLookup(l => fieldExtractor.GetValue(l, condition.LeftField));

        // RIGHT OUTER JOIN - all right rows, matching left rows (or default)
        foreach (var rightRecord in rightRecords)
        {
            var rightValue = fieldExtractor.GetValue(rightRecord, condition.RightField);
            var matchingLefts = leftLookup[rightValue].ToList();

            if (matchingLefts.Count > 0)
            {
                foreach (var leftRecord in matchingLefts)
                {
                    yield return resultSelector(leftRecord, rightRecord);
                }
            }
            else
            {
                yield return resultSelector(default!, rightRecord);
            }
        }
    }
}
