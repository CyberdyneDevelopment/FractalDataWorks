using Fdw.Collections.Attributes;

namespace Fdw.Services.Execution.Abstractions.OptionTypes.States;

/// <summary>
/// The process has been triggered but has not yet started execution.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ProcessStates), "Pending", RestrictToCurrentCompilation = true)]
public sealed class Pending : ProcessStateBase
{
    /// <summary>
    /// Initializes a new instance of the Pending state.
    /// </summary>
    public Pending()
        : base(
            id: 6,
            name: "Pending",
            isTerminal: false,
            isError: false,
            isActive: false,
            isInitial: false)
    {
    }
}
