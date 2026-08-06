using Fdw.Collections;
using Microsoft.Extensions.Logging;

namespace Fdw.Results.Abstractions;

/// <summary>
/// Interface for typed result codes.
/// </summary>
public interface IResultCode : ITypeOption<int, ResultCodeBase>
{
    /// <summary>
    /// Gets the string code identifier — <c>{prefix}-{number}</c>, e.g. <c>MESSAGING-91000</c>.
    /// </summary>
    string Code { get; }

    /// <summary>
    /// Gets the event ID for logging (matches MessageLogging EventId pattern).
    /// </summary>
    int EventId { get; }

    /// <summary>
    /// Gets the severity of this result code.
    /// </summary>
    IResultSeverity Severity { get; }

    /// <summary>
    /// Gets the LogLevel corresponding to this result code's severity.
    /// </summary>
    LogLevel LogLevel { get; }

    /// <summary>
    /// Gets the domain this result code belongs to.
    /// </summary>
    string Domain { get; }

    /// <summary>
    /// Gets the message template for this result code.
    /// </summary>
    string MessageTemplate { get; }

    /// <summary>
    /// Gets whether this result code indicates a retryable operation.
    /// </summary>
    bool IsRetryable { get; }

    /// <summary>
    /// Formats the message with the provided details.
    /// </summary>
    /// <param name="details">The details to format into the message.</param>
    /// <returns>The formatted message.</returns>
    string FormatMessage(IResultDetails? details = null);

    /// <summary>
    /// Logs this result code with the provided details to the logger.
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="details">Optional details to include in the message.</param>
    void Log(ILogger logger, IResultDetails? details = null);

    /// <summary>
    /// Logs this result code with the provided details and returns self for fluent chaining.
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="details">Optional details to include in the message.</param>
    /// <returns>This result code instance.</returns>
    IResultCode LogAndReturn(ILogger logger, IResultDetails? details = null);
}
