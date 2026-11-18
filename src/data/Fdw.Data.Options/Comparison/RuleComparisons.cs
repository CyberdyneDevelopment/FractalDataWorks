using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Options;

/// <summary>
/// The first child collection of the DataConfigurationRules root (FDW-6340): the kinds of comparison a rule can state, each of which
/// adds one condition to a filter. A path reads <c>DataConfigurationRules:Comparison:In.To</c>.
/// </summary>
[TypeCollection(typeof(RuleComparisonBase), typeof(IRuleComparison), typeof(RuleComparisons),
    TypeOption = typeof(DataConfigurationRules), TypeOptionName = "Comparison")]
public abstract partial class RuleComparisons : TypeCollectionBase<RuleComparisonBase, IRuleComparison>
{
}
