using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>A run that claims nothing: it writes no row and is claimed at once.</summary>
[TypeOption(typeof(ClaimKeyTypes), "Alone")]
public sealed class AloneClaimKeyType : ClaimKeyTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="AloneClaimKeyType"/> class.</summary>
    public AloneClaimKeyType()
        : base("Alone", "A run that claims nothing: it writes no row and is claimed at once.", holdsRow: false, statesCap: false)
    {
    }
}
