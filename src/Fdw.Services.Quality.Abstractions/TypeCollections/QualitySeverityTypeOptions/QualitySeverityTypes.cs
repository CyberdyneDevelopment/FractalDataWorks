using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.QualitySeverityTypeOptions;

/// <summary>
/// TypeCollection for quality severity types.
/// Source generator will populate with all discovered TypeOptions.
/// </summary>
[TypeCollection(typeof(QualitySeverityTypeBase), typeof(IQualitySeverityType), typeof(QualitySeverityTypes))]
public sealed partial class QualitySeverityTypes : TypeCollectionBase<QualitySeverityTypeBase, IQualitySeverityType>
{
}
