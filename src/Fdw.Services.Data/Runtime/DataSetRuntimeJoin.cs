using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
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

        var resolvedType = JoinTypes.ByName(config.JoinType);
        if (ReferenceEquals(resolvedType, JoinTypes.NotFound))
            throw new ArgumentException(
                $"Unknown join type '{config.JoinType}'. Valid values: {string.Join(", ", JoinTypes.All().Select(t => t.Name))}",
                nameof(config));

        Type = resolvedType;

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
