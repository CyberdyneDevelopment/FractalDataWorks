using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Execution.Abstractions.OptionTypes.States;

/// <summary>
/// The process exceeded its execution timeout limit.
/// This is a terminal error state - no further transitions are possible.
/// </summary>
[TypeOption(typeof(ProcessStates), "TimedOut", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TimedOut : ProcessStateBase
{
    /// <summary>
    /// Initializes a new instance of the TimedOut state.
    /// </summary>
    public TimedOut()
        : base(
            id: 7,
            name: "TimedOut",
            isTerminal: true,
            isError: true,
            isActive: false,
            isInitial: false)
    {
    }
}
