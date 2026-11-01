using Fdw.Collections;

namespace Fdw.Messages;

/// <summary>
/// Base class for framework message severity levels.
/// </summary>
public abstract class MessageSeverityBase : TypeOptionBase<int, MessageSeverityBase>, IMessageSeverity
{
    /// <summary>
    /// Initializes a new instance of <see cref="MessageSeverityBase"/>.
    /// </summary>
    protected MessageSeverityBase(int id, string name) : base(id, name) { }
}
