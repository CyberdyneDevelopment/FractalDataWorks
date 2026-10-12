using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>The axes a field's data can sit on in a chart: a category, a number, a time, a place.</summary>
/// <remarks>
/// A datum type declares which of these it can take (<see cref="IDatumType.Axes"/>), so the chart check, the data
/// route and a workbench that offers choices all read one answer. No option may be named Category: the generated
/// collection would expose it as a static property that hides the inherited member (CS0108). Hence Dimension.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(AxisTypeBase), typeof(IAxisType), typeof(AxisTypes))]
public abstract partial class AxisTypes : TypeCollectionBase<AxisTypeBase, IAxisType>
{
}
