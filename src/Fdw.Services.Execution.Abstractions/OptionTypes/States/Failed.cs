using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Execution.Abstractions.OptionTypes.States;

/// <summary>
/// Final state when a process has failed due to an error.
/// </summary>
[TypeOption(typeof(ProcessStates), "Failed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class Failed : ProcessStateBase
{
    /// <summary>
    /// Initializes a new instance of the Failed state.
    /// </summary>
    public Failed() : base(4, "Failed", isTerminal: true, isError: true, isActive: false, isInitial: false)
    {
    }
}