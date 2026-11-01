using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Base class for the <see cref="VectorMetrics"/> options.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class VectorMetricBase : TypeOptionBase<VectorMetricBase>, IVectorMetric
{
    /// <summary>
    /// Initializes the NotFound sentinel.
    /// </summary>
    protected VectorMetricBase()
        : base(0, "NotFound")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VectorMetricBase"/> class.
    /// </summary>
    /// <param name="name">The metric's name.</param>
    /// <param name="displayName">The metric's name as a person reads it.</param>
    /// <param name="description">What the distance measures.</param>
    protected VectorMetricBase(string name, string displayName, string description)
        : base(name, $"VectorMetrics:{name}", displayName, description, "VectorMetric")
    {
    }
}
