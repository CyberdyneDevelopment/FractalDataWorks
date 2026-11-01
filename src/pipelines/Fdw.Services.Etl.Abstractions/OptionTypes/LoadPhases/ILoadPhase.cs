using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// When in a run one load row of a graph destination is written, and what it writes: Load or Reject (FDW-1142).
/// </summary>
public interface ILoadPhase : ITypeOption<int, ILoadPhase>
{
    /// <summary>Gets whether the row writes the records the pipeline's stages rejected (Reject) instead of its transformed output rows (Load).</summary>
    bool ReadsRejectedRecords { get; }
}
