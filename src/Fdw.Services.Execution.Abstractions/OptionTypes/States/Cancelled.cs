using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Execution.Abstractions.OptionTypes.States;

/// <summary>
/// Final state when a process has been cancelled before completion.
/// </summary>
[TypeOption(typeof(ProcessStates), "Cancelled", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class Cancelled : ProcessStateBase
{
    /// <summary>
    /// Initializes a new instance of the Cancelled state.
    /// </summary>
    public Cancelled() : base(5, "Cancelled", isTerminal: true, isError: false, isActive: false, isInitial: false)
    {
    }
}