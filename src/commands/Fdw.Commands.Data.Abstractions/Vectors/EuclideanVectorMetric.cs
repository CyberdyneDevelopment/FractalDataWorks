using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>The straight-line distance between the vectors' end points.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(VectorMetrics), "Euclidean")]
public sealed class EuclideanVectorMetric : VectorMetricBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EuclideanVectorMetric"/> class.
    /// </summary>
    public EuclideanVectorMetric()
        : base("Euclidean", "Euclidean", "The straight-line distance between the vectors' end points.")
    {
    }
}
