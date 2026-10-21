using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>The negated dot product of the vectors, so the nearer is still the smaller.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(VectorMetrics), "Dot")]
public sealed class DotVectorMetric : VectorMetricBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DotVectorMetric"/> class.
    /// </summary>
    public DotVectorMetric()
        : base("Dot", "Dot product", "The negated dot product of the vectors: the larger the product, the nearer.")
    {
    }
}
