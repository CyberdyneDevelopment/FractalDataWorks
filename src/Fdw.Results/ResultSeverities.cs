using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// TypeCollection for result severity levels.
/// LogLevel values: Trace=0, Debug=1, Information=2, Warning=3, Error=4, Critical=5, None=6
/// </summary>
[TypeCollection(typeof(ResultSeverityBase), typeof(IResultSeverity), typeof(ResultSeverities))]
[ExcludeFromCodeCoverage]
public abstract partial class ResultSeverities : TypeCollectionBase<ResultSeverityBase, IResultSeverity>
{
}
