using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>Inner join semantics — a missing lookup key fails the record.</summary>
[TypeOption(typeof(LookupJoinTypes), "Inner")]
public sealed class InnerLookupJoinType : LookupJoinTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="InnerLookupJoinType"/> class.</summary>
    public InnerLookupJoinType() : base(1, "Inner", failOnMissing: true)
    {
    }
}
