using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
// Why: Use alias to avoid IDataField ambiguity — both Fdw.Data.Abstractions and
// Fdw.Data.DataSets.Abstractions define IDataField with different contracts.
// JoinTypes lives in Fdw.Data.Abstractions and is already available via that import.
using JoinConfiguration = Fdw.Data.DataSets.Abstractions.JoinConfiguration;

namespace Fdw.Services.Data.Runtime;

/// <summary>
/// Runtime implementation of <see cref="IDataSetJoin"/> built from a <c>JoinConfiguration</c> record
/// and the resolved source map for the parent <c>DataSetConfiguration</c>.
/// </summary>
internal sealed class DataSetRuntimeJoin : IDataSetJoin
{
    /// <summary>
    /// Initializes a new <see cref="DataSetRuntimeJoin"/> from the given join configuration and source map.
    /// </summary>
    /// <param name="config">The join configuration record.</param>
    /// <param name="sourcesByName">
    /// A map of source name → <see cref="IDataSetSource"/> for the parent DataSet.
    /// Used to resolve <see cref="Left"/> and <see cref="Right"/> from the source names stored in config.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the left or right source name in <paramref name="config"/> cannot be resolved
    /// from <paramref name="sourcesByName"/>.
    /// </exception>
    public DataSetRuntimeJoin(JoinConfiguration config, IReadOnlyDictionary<string, IDataSetSource> sourcesByName)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(sourcesByName);

        if (!sourcesByName.TryGetValue(config.LeftSource, out var left))
            throw new ArgumentException(
                $"Left source '{config.LeftSource}' not found in DataSet source map.", nameof(config));

        if (!sourcesByName.TryGetValue(config.RightSource, out var right))
            throw new ArgumentException(
                $"Right source '{config.RightSource}' not found in DataSet source map.", nameof(config));

        Left = left;
        Right = right;

        // Why: JoinConfiguration uses plain string "Inner"/"Left"/"Right"/"Full".
        // JoinTypes.ByName resolves to the canonical TypeCollection entry. NotFound sentinel
        // is handled below — rather than silently defaulting to Inner, we fail loudly.
        var resolvedType = JoinTypes.ByName(config.JoinType);
        if (ReferenceEquals(resolvedType, JoinTypes.NotFound))
            throw new ArgumentException(
                $"Unknown join type '{config.JoinType}'. Valid values: {string.Join(", ", JoinTypes.All().Select(t => t.Name))}",
                nameof(config));

        Type = resolvedType;

        // Why: Condition is a field-equality filter between Left.Node.{LeftField} and Right.Node.{RightField}.
        // A concrete IFilterExpression is not built here because the filter expression model requires
        // resolved field references (IDataField), which are only available after the DataStore tree is
        // loaded. The factory sets this to null; the DataGatewayService uses LeftFieldName/RightFieldName
        // directly from configuration when building the physical join query.
        // This matches the pattern used in DataSet sources: physical details are resolved at query time.
        Condition = NullFilterExpression.Instance;
    }

    /// <inheritdoc />
    public IDataSetSource Left { get; }

    /// <inheritdoc />
    public IDataSetSource Right { get; }

    /// <inheritdoc />
    public IFilterExpression Condition { get; }

    /// <inheritdoc />
    public IJoinType Type { get; }
}
