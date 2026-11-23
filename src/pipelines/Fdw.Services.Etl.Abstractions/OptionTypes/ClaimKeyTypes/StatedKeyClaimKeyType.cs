using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>A key the pipeline states: the claim service always begins it with the tenant's id, so no template can leave the tenant out.</summary>
[TypeOption(typeof(ClaimKeyTypes), "StatedKey")]
public sealed class StatedKeyClaimKeyType : ClaimKeyTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="StatedKeyClaimKeyType"/> class.</summary>
    public StatedKeyClaimKeyType()
        : base("StatedKey", "A key the pipeline states, always beginning with the tenant's id; one run holds it at a time.", holdsRow: true, statesCap: false)
    {
    }
}
