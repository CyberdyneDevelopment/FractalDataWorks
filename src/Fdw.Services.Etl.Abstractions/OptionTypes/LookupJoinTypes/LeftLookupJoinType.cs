using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>Left join semantics — a missing lookup key leaves the output field null.</summary>
[TypeOption(typeof(LookupJoinTypes), "Left")]
public sealed class LeftLookupJoinType : LookupJoinTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="LeftLookupJoinType"/> class.</summary>
    public LeftLookupJoinType() : base(2, "Left", failOnMissing: false)
    {
    }
}
