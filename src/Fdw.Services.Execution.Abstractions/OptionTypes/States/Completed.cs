using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Execution.Abstractions.OptionTypes.States;

/// <summary>
/// Final state when a process has completed successfully.
/// </summary>
[TypeOption(typeof(ProcessStates), "Completed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class Completed : ProcessStateBase
{
    /// <summary>
    /// Initializes a new instance of the Completed state.
    /// </summary>
    public Completed() : base(3, "Completed", isTerminal: true, isError: false, isActive: false, isInitial: false)
    {
    }
}