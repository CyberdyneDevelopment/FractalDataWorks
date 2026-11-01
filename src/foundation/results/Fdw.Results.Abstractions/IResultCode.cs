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
    /// Formats the part of this code's message a caller may be shown in an HTTP response, or null when none is.
    /// </summary>
    /// <param name="details">The details to format into the message.</param>
    /// <returns>The text to show, or null, which is what almost every code returns.</returns>
    /// <remarks>
    /// <see cref="ResultCodeBase"/> returns null; a code overrides it to opt in. A response carries only its category's generic text by default, because a message is written for the log and can
    /// hold paths, SQL, host names or raw exception text. A validation code whose message is written for the caller of a
    /// refused request (a field that does not exist, a start after an end) overrides this in its own class and says what
    /// it is willing to show. The response mapper shows it for the validation category only.
    /// </remarks>
    string? FormatClientMessage(IResultDetails? details = null);

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
