using System;
using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Abstractions.FieldAccess;
using Fdw.Commands.Data.FieldAccess;
using Fdw.Commands.Data.Joins;

namespace Fdw.Commands.Data;

/// <summary>
/// Merges results from multiple data sources using join algorithms.
/// Delegates to <see cref="JoinExecutors"/> TypeCollection for strategy-based execution.
/// </summary>
public sealed class ResultMerger : IResultMerger
{
    private readonly IFieldValueExtractor _fieldExtractor;
    private readonly IQualifiedNameParser _nameParser;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultMerger"/> class
    /// with default field extractor and name parser.
    /// </summary>
    public ResultMerger()
        : this(new CompositeFieldExtractor(), new QualifiedNameParser())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultMerger"/> class.
    /// </summary>
    /// <param name="fieldExtractor">The field value extractor.</param>
    /// <param name="nameParser">The qualified name parser.</param>
    public ResultMerger(IFieldValueExtractor fieldExtractor, IQualifiedNameParser nameParser)
    {
        _fieldExtractor = fieldExtractor ?? throw new ArgumentNullException(nameof(fieldExtractor));
        _nameParser = nameParser ?? throw new ArgumentNullException(nameof(nameParser));
    }

    /// <summary>
    /// Performs a hash join between two data sources.
    /// </summary>
    /// <typeparam name="TLeft">The left source record type.</typeparam>
    /// <typeparam name="TRight">The right source record type.</typeparam>
    /// <typeparam name="TResult">The result type after merging.</typeparam>
    /// <param name="leftRecords">Records from the left source.</param>
    /// <param name="rightRecords">Records from the right source.</param>
    /// <param name="joinDefinition">The join definition specifying how to join.</param>
    /// <param name="resultSelector">Function to create result from left and right records.</param>
    /// <returns>Merged records.</returns>
    public IEnumerable<TResult> HashJoin<TLeft, TRight, TResult>(
        IEnumerable<TLeft> leftRecords,
        IEnumerable<TRight> rightRecords,
        IJoinDefinition joinDefinition,
        Func<TLeft, TRight, TResult> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(leftRecords);
        ArgumentNullException.ThrowIfNull(rightRecords);
        ArgumentNullException.ThrowIfNull(joinDefinition);
        ArgumentNullException.ThrowIfNull(resultSelector);

        if (joinDefinition.Conditions.Count == 0)
        {
            throw new InvalidOperationException("Join definition must have at least one condition");
        }

        var rawCondition = joinDefinition.Conditions[0];
        var condition = (
            _nameParser.GetFieldName(rawCondition.LeftField),
            _nameParser.GetFieldName(rawCondition.RightField));

        // Lookup executor from TypeCollection by join type name
        var executor = JoinExecutors.ByName(joinDefinition.JoinType.Name);
        if (executor == JoinExecutors.NotFound)
        {
            throw new NotSupportedException($"Join type '{joinDefinition.JoinType.Name}' is not supported");
        }

        return executor.Execute(leftRecords, rightRecords, _fieldExtractor, condition, resultSelector);
    }

    /// <summary>
    /// Performs a nested loop join between two data sources.
    /// </summary>
    /// <typeparam name="TLeft">The left source record type.</typeparam>
    /// <typeparam name="TRight">The right source record type.</typeparam>
    /// <typeparam name="TResult">The result type after merging.</typeparam>
    /// <param name="leftRecords">Records from the left source.</param>
    /// <param name="rightRecords">Records from the right source.</param>
    /// <param name="joinDefinition">The join definition specifying how to join.</param>
    /// <param name="resultSelector">Function to create result from left and right records.</param>
    /// <returns>Merged records.</returns>
    /// <remarks>
    /// Nested loop join uses O(n*m) algorithm. For large datasets, prefer HashJoin.
    /// Currently supports Inner, Left, and Cross joins only.
    /// </remarks>
    public IEnumerable<TResult> NestedLoopJoin<TLeft, TRight, TResult>(
        IEnumerable<TLeft> leftRecords,
        IEnumerable<TRight> rightRecords,
        IJoinDefinition joinDefinition,
        Func<TLeft, TRight, TResult> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(leftRecords);
        ArgumentNullException.ThrowIfNull(rightRecords);
        ArgumentNullException.ThrowIfNull(joinDefinition);
        ArgumentNullException.ThrowIfNull(resultSelector);

        if (joinDefinition.Conditions.Count == 0)
        {
            throw new InvalidOperationException("Join definition must have at least one condition");
        }

        var rawCondition = joinDefinition.Conditions[0];
        var condition = (
            _nameParser.GetFieldName(rawCondition.LeftField),
            _nameParser.GetFieldName(rawCondition.RightField));

        var joinTypeName = joinDefinition.JoinType.Name;

        // Nested loop join only supports a subset of join types
        if (!string.Equals(joinTypeName, "Inner", StringComparison.Ordinal) &&
            !string.Equals(joinTypeName, "Left", StringComparison.Ordinal) &&
            !string.Equals(joinTypeName, "Cross", StringComparison.Ordinal))
        {
            throw new NotSupportedException($"Nested loop join for type '{joinTypeName}' not yet implemented");
        }

        // Lookup executor from TypeCollection
        var executor = JoinExecutors.ByName(joinTypeName);
        if (executor == JoinExecutors.NotFound)
        {
            throw new NotSupportedException($"Join type '{joinTypeName}' is not supported");
        }

        // Note: This still uses hash-based execution from the executor.
        // For true nested loop semantics, would need separate NestedLoopExecutor implementations.
        return executor.Execute(leftRecords, rightRecords, _fieldExtractor, condition, resultSelector);
    }
}
