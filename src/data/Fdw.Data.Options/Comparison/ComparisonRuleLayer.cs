using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Options;

/// <summary>Opens the comparison kinds under the DataConfigurationRules root (FDW-6340).</summary>
[TypeOption(typeof(DataConfigurationRules), "Comparison")]
public sealed class ComparisonRuleLayer : RuleLayerBase
{
    /// <summary>Initializes a new instance of the <see cref="ComparisonRuleLayer"/> class.</summary>
    public ComparisonRuleLayer()
        : base("Comparison", "Comparison", "Keeps the rows whose column compares a certain way with the values you pick.")
    {
    }

    /// <inheritdoc />
    public override IReadOnlyList<IRuleLayer> Layers => [.. RuleComparisons.All().Cast<IRuleLayer>()];
}
