using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// A run whose writes cannot be told (FDW-6939): it holds one row that conflicts with every other claim in the operational database, in every tenant, and every other claim
/// conflicts with it, in the order the runs asked. It keeps the rule the scheduler has always had for such a run: it runs alone.
/// </summary>
[TypeOption(typeof(ClaimKeyTypes), "Exclusive")]
public sealed class ExclusiveClaimKeyType : ClaimKeyTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ExclusiveClaimKeyType"/> class.</summary>
    public ExclusiveClaimKeyType()
        : base("Exclusive", "A run whose writes cannot be told: it conflicts with every other claim in the operational database and runs alone.", holdsRow: true, statesCap: false)
    {
    }
}
