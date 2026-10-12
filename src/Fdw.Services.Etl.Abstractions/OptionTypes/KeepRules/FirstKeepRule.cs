using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>Keeps the row with the smallest order value; of equal values, the one met first.</summary>
[TypeOption(typeof(KeepRules), "First")]
public sealed class FirstKeepRule : KeepRuleBase
{
    /// <summary>Initializes a new instance of the <see cref="FirstKeepRule"/> class.</summary>
    public FirstKeepRule() : base("First", keepsLargest: false)
    {
    }
}
