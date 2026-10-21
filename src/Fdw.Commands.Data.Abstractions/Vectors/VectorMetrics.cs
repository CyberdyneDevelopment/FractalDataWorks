using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// How a nearest search measures the distance between two vectors: Cosine, Euclidean or Dot.
/// </summary>
[TypeCollection(typeof(VectorMetricBase), typeof(IVectorMetric), typeof(VectorMetrics))]
[ExcludeFromCodeCoverage]
public abstract partial class VectorMetrics : TypeCollectionBase<VectorMetricBase, IVectorMetric>
{
}
