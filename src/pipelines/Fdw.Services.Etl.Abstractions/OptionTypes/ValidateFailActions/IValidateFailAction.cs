using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// What a Validate stage does with a record that fails one of its checks: Reject or Fail (FDW-1120).
/// </summary>
public interface IValidateFailAction : ITypeOption<int, IValidateFailAction>
{
    /// <summary>Gets whether a failing record stops the run (Fail) instead of leaving the stream (Reject).</summary>
    bool StopsRun { get; }
}
