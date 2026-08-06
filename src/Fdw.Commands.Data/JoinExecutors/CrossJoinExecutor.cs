using System;
using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions.FieldAccess;
using Fdw.Commands.Data.Abstractions.JoinExecutors;

namespace Fdw.Commands.Data.Joins;

/// <summary>
/// Executes a cross join (Cartesian product) between two record sets.
/// Returns all possible combinations of left and right records.
/// </summary>
[TypeOption(typeof(JoinExecutors), "Cross")]
public sealed class CrossJoinExecutor : JoinExecutorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CrossJoinExecutor"/> class.
    /// </summary>
    public CrossJoinExecutor() : base(5, "Cross") { }

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

        // CROSS JOIN - Cartesian product (condition is ignored)
        foreach (var leftRecord in leftRecords)
        {
            foreach (var rightRecord in rightRecords)
            {
                yield return resultSelector(leftRecord, rightRecord);
            }
        }
    }
}
