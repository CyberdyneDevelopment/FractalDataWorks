using System;
using System.Collections.Generic;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Merges results from multiple data sources using join algorithms.
/// </summary>
public interface IResultMerger
{
    /// <summary>
    /// Performs a hash join between two data sources.
    /// </summary>
    /// <typeparam name="TLeft">The left source record type.</typeparam>
    /// <typeparam name="TRight">The right source record type.</typeparam>
    /// <typeparam name="TResult">The result type after merging.</typeparam>
    /// <param name="leftRecords">Records from the left source.</param>
    /// <param name="rightRecords">Records from the right source.</param>
    /// <param name="joinDefinition">The join definition specifying how to join.</param>
    /// <param name="resultSelector">Function to create result from left and right records. Right may be default for outer joins.</param>
    /// <returns>Merged records.</returns>
    IEnumerable<TResult> HashJoin<TLeft, TRight, TResult>(
        IEnumerable<TLeft> leftRecords,
        IEnumerable<TRight> rightRecords,
        IJoinDefinition joinDefinition,
        Func<TLeft, TRight, TResult> resultSelector);

    /// <summary>
    /// Performs a nested loop join between two data sources.
    /// </summary>
    /// <typeparam name="TLeft">The left source record type.</typeparam>
    /// <typeparam name="TRight">The right source record type.</typeparam>
    /// <typeparam name="TResult">The result type after merging.</typeparam>
    /// <param name="leftRecords">Records from the left source.</param>
    /// <param name="rightRecords">Records from the right source.</param>
    /// <param name="joinDefinition">The join definition specifying how to join.</param>
    /// <param name="resultSelector">Function to create result from left and right records. Right may be default for outer joins.</param>
    /// <returns>Merged records.</returns>
    IEnumerable<TResult> NestedLoopJoin<TLeft, TRight, TResult>(
        IEnumerable<TLeft> leftRecords,
        IEnumerable<TRight> rightRecords,
        IJoinDefinition joinDefinition,
        Func<TLeft, TRight, TResult> resultSelector);
}
