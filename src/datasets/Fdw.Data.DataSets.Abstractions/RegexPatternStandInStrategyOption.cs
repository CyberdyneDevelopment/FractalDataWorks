using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>Values match a regular expression.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(StandInStrategies), "RegexPattern")]
public sealed class RegexPatternStandInStrategyOption : StandInStrategyBase
{
    /// <summary>Initializes a new instance of the <see cref="RegexPatternStandInStrategyOption"/> class.</summary>
    public RegexPatternStandInStrategyOption() : base("RegexPattern",
        [
            new StandInParameterSchema { Name = "pattern", Type = "string", Required = true },
        ],
        limitations: "Preview generates a best-effort string honoring literals, character classes and basic quantifiers; it does not implement the full regex grammar (lookaround, backreferences).")
    {
    }
}
