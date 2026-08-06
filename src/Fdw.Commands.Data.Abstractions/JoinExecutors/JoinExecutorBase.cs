using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Commands.Data.Abstractions.FieldAccess;

namespace Fdw.Commands.Data.Abstractions.JoinExecutors;

/// <summary>
/// Base class for join executor implementations.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class JoinExecutorBase : TypeOptionBase<int, JoinExecutorBase>, IJoinExecutor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JoinExecutorBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this join executor.</param>
    /// <param name="name">The name of this join executor (must match JoinType name).</param>
    protected JoinExecutorBase(int id, string name)
        : base(id, name) { }

    /// <inheritdoc />
    public abstract IEnumerable<TResult> Execute<TLeft, TRight, TResult>(
        IEnumerable<TLeft> leftRecords,
        IEnumerable<TRight> rightRecords,
        IFieldValueExtractor fieldExtractor,
        (string LeftField, string RightField) condition,
        Func<TLeft, TRight, TResult> resultSelector);
}
