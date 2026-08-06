using Fdw.Collections;

namespace Fdw.Results.ExecutionStatus;

/// <summary>
/// Interface for execution status types with UI rendering properties.
/// </summary>
/// <remarks>
/// <para>
/// Execution status types provide metadata for rendering pipeline, schedule, and workflow
/// execution states in UI components. Each status includes visual properties for consistent display.
/// </para>
/// <para>
/// Supported statuses:
/// <list type="bullet">
/// <item><description>Pending - Execution is queued</description></item>
/// <item><description>Running - Execution in progress</description></item>
/// <item><description>Succeeded - Execution completed successfully</description></item>
/// <item><description>Failed - Execution failed with errors</description></item>
/// <item><description>Cancelled - Execution was cancelled</description></item>
/// <item><description>Skipped - Execution was skipped</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IExecutionStatus : ITypeOption<int, ExecutionStatusBase>
{
    /// <summary>
    /// Gets the MudBlazor icon name for this status.
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// Gets the MudBlazor color for this status.
    /// </summary>
    /// <example>Primary, Success, Info, Warning, Error</example>
    string Color { get; }

    /// <summary>
    /// Gets whether this is a terminal state (no more transitions possible).
    /// </summary>
    bool IsTerminal { get; }

    /// <summary>
    /// Gets whether this status indicates success.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets whether this status indicates the execution is still in progress.
    /// </summary>
    bool IsInProgress { get; }
}
