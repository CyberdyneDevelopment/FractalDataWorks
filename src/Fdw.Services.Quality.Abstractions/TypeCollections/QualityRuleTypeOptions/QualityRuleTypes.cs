using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.QualityRuleTypeOptions;

/// <summary>
/// TypeCollection for quality rule types.
/// Source generator will populate with all discovered TypeOptions.
/// </summary>
[TypeCollection(typeof(QualityRuleTypeBase), typeof(IQualityRuleType), typeof(QualityRuleTypes))]
public sealed partial class QualityRuleTypes : TypeCollectionBase<QualityRuleTypeBase, IQualityRuleType>
{
}
