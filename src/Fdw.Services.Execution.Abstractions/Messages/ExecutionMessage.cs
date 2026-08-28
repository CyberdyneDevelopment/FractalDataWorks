using Fdw.Messages;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Execution.Abstractions.Messages;

/// <summary>
/// Base class for all Execution-related messages.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public abstract class ExecutionMessage : MessageTemplate<MessageSeverity>, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionMessage"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this message type.</param>
    /// <param name="name">The name of this message type.</param>
    /// <param name="severity">The severity level of the message.</param>
    /// <param name="message">The human-readable message text.</param>
    /// <param name="code">The message code or identifier (optional).</param>
    /// <param name="category">The category for grouping (optional).</param>
    /// <param name="helpLink">Link to documentation (optional).</param>
    protected ExecutionMessage(int id, string name, MessageSeverity severity,
        string message, string? code = null, string? category = null, string? helpLink = null)
        : base(id, name, severity, message, code, "Execution") { }
}