using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// What a Branch stage does with a record that matches none of its paths: Pass or Reject (FDW-1120).
/// </summary>
public interface IUnmatchedAction : ITypeOption<int, IUnmatchedAction>
{
    /// <summary>Gets whether an unmatched record leaves the stream and is reported (Reject) instead of passing on (Pass).</summary>
    bool RejectsRecord { get; }
}
