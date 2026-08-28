using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Data.Abstractions.Messages;

/// <summary>
/// Base class for all data-related messages.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[MessageCollection("DataMessages")]
public abstract class DataMessage : MessageTemplate<MessageSeverity>, IDataMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataMessage"/> class.
    /// </summary>
    /// <param name="id">The message identifier.</param>
    /// <param name="name">The message name.</param>
    /// <param name="severity">The message severity.</param>
    /// <param name="message">The message text.</param>
    /// <param name="code">The optional error code.</param>
    protected DataMessage(int id, string name, MessageSeverity severity,
        string message, string? code = null)
        : base(id, name, severity, message, code, "Data", null, null)
    {
    }
}
