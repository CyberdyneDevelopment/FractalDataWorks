using Fdw.Collections;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// How the distance between two vectors is measured. A smaller distance is always the nearer.
/// </summary>
public interface IVectorMetric : ITypeOption<int, VectorMetricBase>
{
}
