using Fdw.Collections;
using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Data.Abstractions.Visualization;

/// <summary>
/// TypeCollection for visualization types. Source generator creates static lookup properties.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(VisualizationTypeBase), typeof(IVisualizationType), typeof(VisualizationTypes))]
public sealed partial class VisualizationTypes : TypeCollectionBase<VisualizationTypeBase, IVisualizationType>
{
}
