using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>One less the cosine of the angle between the vectors: 0 for the same direction, 2 for opposite ones.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(VectorMetrics), "Cosine")]
public sealed class CosineVectorMetric : VectorMetricBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CosineVectorMetric"/> class.
    /// </summary>
    public CosineVectorMetric()
        : base("Cosine", "Cosine", "One less the cosine of the angle between the vectors; their lengths do not count.")
    {
    }
}
