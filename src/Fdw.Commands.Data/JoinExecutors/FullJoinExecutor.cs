using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions.FieldAccess;
using Fdw.Commands.Data.Abstractions.JoinExecutors;

namespace Fdw.Commands.Data.Joins;

/// <summary>
/// Executes a full outer join between two record sets.
/// Returns all rows from both sources, with defaults where no match exists.
/// </summary>
[TypeOption(typeof(JoinExecutors), "Full")]
public sealed class FullJoinExecutor : JoinExecutorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FullJoinExecutor"/> class.
    /// </summary>
    public FullJoinExecutor() : base(4, "Full") { }

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

        // Materialize both sides (needed for full outer join)
        var leftList = leftRecords.ToList();
        var rightList = rightRecords.ToList();

        // Build lookup for left side
        var leftLookup = leftList
            .ToLookup(l => fieldExtractor.GetValue(l, condition.LeftField));

        // Track which left records have been matched
        var matchedLeftValues = new HashSet<object?>();

        // Process all right records
        foreach (var rightRecord in rightList)
        {
            var rightValue = fieldExtractor.GetValue(rightRecord, condition.RightField);
            var matchingLefts = leftLookup[rightValue].ToList();

            if (matchingLefts.Count > 0)
            {
                foreach (var leftRecord in matchingLefts)
                {
                    var leftValue = fieldExtractor.GetValue(leftRecord, condition.LeftField);
                    matchedLeftValues.Add(leftValue);
                    yield return resultSelector(leftRecord, rightRecord);
                }
            }
            else
            {
                yield return resultSelector(default!, rightRecord);
            }
        }

        // Process unmatched left records
        foreach (var leftRecord in leftList)
        {
            var leftValue = fieldExtractor.GetValue(leftRecord, condition.LeftField);
            if (!matchedLeftValues.Contains(leftValue))
            {
                yield return resultSelector(leftRecord, default!);
            }
        }
    }
}
