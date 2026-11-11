using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// What an absent-key soft delete does at the end of a run when rows were rejected upstream of its destination (FDW-4956): a rejected row's key never
/// reaches the destination, so the sweep would flag its target row as gone from the source. Refuse or Proceed, stated by the destination; no default.
/// </summary>
public interface ISweepRejectedRowsRule : ITypeOption<int, ISweepRejectedRowsRule>
{
    /// <summary>Gets a value indicating whether the sweep runs although rows were rejected upstream (Proceed) instead of failing the destination (Refuse).</summary>
    bool SweepsAfterRejections { get; }
}
