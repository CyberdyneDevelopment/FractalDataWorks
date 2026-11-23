using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// The kinds of key a pipeline run claims (FDW-6629): a table or file it writes, a key its pipeline states, the cap of its pipeline, or nothing. The claim procedures compare keys of one kind
/// with keys of the same kind.
/// </summary>
public interface IClaimKeyType : ITypeOption<int, IClaimKeyType>
{
    /// <summary>Gets whether a run of this kind writes a row in the claim table. A run that claims nothing writes none.</summary>
    bool HoldsRow { get; }

    /// <summary>Gets whether a key of this kind states a cap: how many runs of one pipeline may be claimed at once.</summary>
    bool StatesCap { get; }
}
