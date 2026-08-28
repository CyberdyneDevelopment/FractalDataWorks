using Fdw.Collections.Attributes;

namespace Fdw.Services.Execution.Abstractions.OptionTypes.States;

/// <summary>
/// State when a process is actively executing.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ProcessStates), "Running", RestrictToCurrentCompilation = true)]
public sealed class Running : ProcessStateBase
{
    /// <summary>
    /// Initializes a new instance of the Running state.
    /// </summary>
    public Running() : base(2, "Running", isTerminal: false, isError: false, isActive: true, isInitial: false)
    {
    }
}