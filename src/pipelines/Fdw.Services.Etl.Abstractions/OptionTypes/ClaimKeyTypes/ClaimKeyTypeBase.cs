using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>Base class for the options of <see cref="ClaimKeyTypes"/>, using the CRTP pattern.</summary>
public abstract class ClaimKeyTypeBase : TypeOptionBase<ClaimKeyTypeBase>, IClaimKeyType
{
    /// <summary>Initializes a new instance of the <see cref="ClaimKeyTypeBase"/> class.</summary>
    /// <param name="name">The option name; it is also the <c>KeyType</c> the claim table stores.</param>
    /// <param name="description">What the kind of key claims.</param>
    /// <param name="holdsRow">Whether a claim of this kind writes a row.</param>
    /// <param name="statesCap">Whether a key of this kind states a cap.</param>
    protected ClaimKeyTypeBase(string name, string description, bool holdsRow, bool statesCap)
        : base(name, name, description, "ClaimKeyTypes")
    {
        HoldsRow = holdsRow;
        StatesCap = statesCap;
    }

    /// <inheritdoc />
    public bool HoldsRow { get; }

    /// <inheritdoc />
    public bool StatesCap { get; }
}
