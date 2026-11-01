using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>Keeps the row with the largest order value; of equal values, the one met last.</summary>
[TypeOption(typeof(KeepRules), "Last")]
public sealed class LastKeepRule : KeepRuleBase
{
    /// <summary>Initializes a new instance of the <see cref="LastKeepRule"/> class.</summary>
    public LastKeepRule() : base("Last", keepsLargest: true)
    {
    }
}
