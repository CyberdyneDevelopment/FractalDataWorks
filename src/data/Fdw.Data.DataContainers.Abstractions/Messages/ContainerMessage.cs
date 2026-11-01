using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Data.DataContainers.Abstractions.Messages;

/// <summary>
/// Base class for all data container-related messages.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[MessageCollection("ContainerMessages")]
public abstract class ContainerMessage : MessageTemplate<MessageSeverity>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerMessage"/> class.
    /// </summary>
    /// <param name="id">The message identifier.</param>
    /// <param name="name">The message name.</param>
    /// <param name="severity">The message severity level.</param>
    /// <param name="message">The message text.</param>
    /// <param name="code">The optional message code.</param>
    protected ContainerMessage(int id, string name, MessageSeverity severity,
        string message, string? code = null)
        : base(id, name, severity, message, code, "Container", null, null) { }
}
