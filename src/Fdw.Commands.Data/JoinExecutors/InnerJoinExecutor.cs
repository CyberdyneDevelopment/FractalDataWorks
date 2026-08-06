using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions.FieldAccess;
using Fdw.Commands.Data.Abstractions.JoinExecutors;

namespace Fdw.Commands.Data.Joins;

/// <summary>
/// Executes an inner join between two record sets.
/// Only returns rows where a match exists in both sources.
/// </summary>
[TypeOption(typeof(JoinExecutors), "Inner")]
public sealed class InnerJoinExecutor : JoinExecutorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InnerJoinExecutor"/> class.
    /// </summary>
    public InnerJoinExecutor() : base(1, "Inner") { }

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

        // INNER JOIN - only matching rows
        foreach (var leftRecord in leftRecords)
        {
            var leftValue = fieldExtractor.GetValue(leftRecord, condition.LeftField);
            var matchingRights = rightLookup[leftValue];

            foreach (var rightRecord in matchingRights)
            {
                yield return resultSelector(leftRecord, rightRecord);
            }
        }
    }
}
