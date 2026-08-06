using Fdw.Collections;

namespace Fdw.Results.ExecutionStatus;

/// <summary>
/// Base class for execution status type definitions with UI properties.
/// </summary>
public abstract class ExecutionStatusBase : TypeOptionBase<int, ExecutionStatusBase>, IExecutionStatus
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionStatusBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this status.</param>
    /// <param name="name">The name of this status.</param>
    /// <param name="icon">The MudBlazor icon name.</param>
    /// <param name="color">The MudBlazor color.</param>
    /// <param name="isTerminal">Whether this is a terminal state.</param>
    /// <param name="isSuccess">Whether this status indicates success.</param>
    /// <param name="isInProgress">Whether execution is still in progress.</param>
    protected ExecutionStatusBase(
        int id,
        string name,
        string icon,
        string color,
        bool isTerminal,
        bool isSuccess,
        bool isInProgress)
        : base(id, name)
    {
        Icon = icon;
        Color = color;
        IsTerminal = isTerminal;
        IsSuccess = isSuccess;
        IsInProgress = isInProgress;
    }

    /// <inheritdoc/>
    public string Icon { get; }

    /// <inheritdoc/>
    public string Color { get; }

    /// <inheritdoc/>
    public bool IsTerminal { get; }

    /// <inheritdoc/>
    public bool IsSuccess { get; }

    /// <inheritdoc/>
    public bool IsInProgress { get; }
}
