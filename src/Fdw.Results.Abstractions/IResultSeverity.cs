using Fdw.Collections;

namespace Fdw.Results.Abstractions;

/// <summary>
/// Interface for result severity levels with logging integration.
/// </summary>
public interface IResultSeverity : ITypeOption<int, ResultSeverityBase>
{
    /// <summary>
    /// Gets whether this severity indicates a successful operation.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets whether this severity indicates a failure.
    /// </summary>
    bool IsFailure { get; }

    /// <summary>
    /// Gets the equivalent Microsoft.Extensions.Logging.LogLevel value.
    /// </summary>
    int LogLevelValue { get; }

    /// <summary>
    /// Gets whether results with this severity should be logged.
    /// </summary>
    bool ShouldLog { get; }

    /// <summary>
    /// Gets the display color hint for UI rendering (CSS color name or hex).
    /// </summary>
    string ColorHint { get; }
}
