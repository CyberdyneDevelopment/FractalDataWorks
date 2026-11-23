using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>A table or file the run writes: the connection, path and container, compared without regard to case and the same in every tenant, as the scheduler has always kept them.</summary>
[TypeOption(typeof(ClaimKeyTypes), "WriteKey")]
public sealed class WriteKeyClaimKeyType : ClaimKeyTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="WriteKeyClaimKeyType"/> class.</summary>
    public WriteKeyClaimKeyType()
        : base("WriteKey", "A table or file the run writes: the connection, path and container; one run writes it at a time.", holdsRow: true, statesCap: false)
    {
    }
}
