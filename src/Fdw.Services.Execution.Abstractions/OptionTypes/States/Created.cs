using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Execution.Abstractions.OptionTypes.States;

/// <summary>
/// Initial state when a process is first created but not yet started.
/// </summary>
[TypeOption(typeof(ProcessStates), "Created", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class Created : ProcessStateBase
{
    /// <summary>
    /// Initializes a new instance of the Created state.
    /// </summary>
    public Created() : base(1, "Created", isTerminal: false, isError: false, isActive: false, isInitial: true)
    {
    }
}