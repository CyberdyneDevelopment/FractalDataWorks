using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Commands.Data.Abstractions.FieldAccess;

namespace Fdw.Commands.Data.Abstractions.JoinExecutors;

/// <summary>
/// Defines a join execution strategy for merging two record sets.
/// </summary>
public interface IJoinExecutor : ITypeOption<int, JoinExecutorBase>
{
    /// <summary>
    /// Executes the join operation between left and right record sets.
    /// </summary>
    /// <typeparam name="TLeft">The left record type.</typeparam>
    /// <typeparam name="TRight">The right record type.</typeparam>
    /// <typeparam name="TResult">The result type after joining.</typeparam>
    /// <param name="leftRecords">Records from the left source.</param>
    /// <param name="rightRecords">Records from the right source.</param>
    /// <param name="fieldExtractor">Extractor for field values.</param>
    /// <param name="condition">The join condition (left field, right field).</param>
    /// <param name="resultSelector">Function to create result from left and right records. Right may be default for outer joins.</param>
    /// <returns>The joined records.</returns>
    IEnumerable<TResult> Execute<TLeft, TRight, TResult>(
        IEnumerable<TLeft> leftRecords,
        IEnumerable<TRight> rightRecords,
        IFieldValueExtractor fieldExtractor,
        (string LeftField, string RightField) condition,
        Func<TLeft, TRight, TResult> resultSelector);
}
