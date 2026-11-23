using Fdw.Collections;

namespace Fdw.Services.Etl.Execution.AtOnceRecovery;

/// <summary>What the recovery's judgment of a run found half-done came to, and what is left for the caller to do.</summary>
public interface IAtOnceRunEnd : ITypeOption<int, IAtOnceRunEnd>
{
    /// <summary>Gets whether the judgment is settled: the run was ended here, or it is not one this recovery ends; false when the run could not be judged and is left open.</summary>
    bool Settled { get; }

    /// <summary>Gets whether the run was a run carried out at once that was ended here, its row and its records closed.</summary>
    bool EndedHere { get; }
}
