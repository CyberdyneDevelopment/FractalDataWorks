using Fdw.Collections;

namespace Fdw.Services.Execution.Abstractions.OptionTypes;

/// <summary>
/// Base class for process states.
/// States represent the current condition of a process during its lifecycle.
/// Examples: Created, Running, Completed, Failed, Cancelled, etc.
/// </summary>
public abstract class ProcessStateBase : TypeOptionBase<int, IProcessState>, ITypeOption<int, ProcessStateBase>, IProcessState
{
    /// <summary>
    /// Initializes a new instance of the ProcessStateBase class.
    /// </summary>
    /// <param name="id">Unique identifier for this state.</param>
    /// <param name="name">Name of the state.</param>
    /// <param name="isTerminal">Whether this is a terminal state.</param>
    /// <param name="isError">Whether this represents an error state.</param>
    /// <param name="isActive">Whether the process is actively running in this state.</param>
    /// <param name="isInitial">Whether this is the initial state for new processes.</param>
    protected ProcessStateBase(int id, string name, bool isTerminal, bool isError, bool isActive, bool isInitial = false)
        : base(id, name)
    {
        IsTerminal = isTerminal;
        IsError = isError;
        IsActive = isActive;
        IsInitial = isInitial;
    }

    /// <summary>
    /// Whether this is a terminal state (no further transitions possible).
    /// </summary>
    public bool IsTerminal { get; }

    /// <summary>
    /// Whether this state represents an error condition.
    /// </summary>
    public bool IsError { get; }

    /// <summary>
    /// Whether this is the initial state for new processes.
    /// </summary>
    public bool IsInitial { get; }

    /// <summary>
    /// Whether the process is actively running in this state.
    /// </summary>
    public bool IsActive { get; }
}