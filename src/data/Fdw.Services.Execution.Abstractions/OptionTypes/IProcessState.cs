using Fdw.Collections;

namespace Fdw.Services.Execution.Abstractions.OptionTypes;

/// <summary>
/// Interface defining the contract for process state enum options.
/// </summary>
public interface IProcessState : ITypeOption<int, IProcessState>
{
    /// <summary>
    /// Gets whether this is a terminal state (no further transitions possible).
    /// </summary>
    bool IsTerminal { get; }

    /// <summary>
    /// Gets whether this state represents an error condition.
    /// </summary>
    bool IsError { get; }

    /// <summary>
    /// Gets whether this is the initial state for new processes.
    /// </summary>
    bool IsInitial { get; }

    /// <summary>
    /// Gets whether the process is actively running in this state.
    /// </summary>
    bool IsActive { get; }
}