using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messages;

/// <summary>
/// CurrentMessage indicating that no service types have been registered.
/// </summary>
[Message("NoServiceTypesRegistered")]
public sealed class NoServiceTypesRegisteredMessage : ServiceMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoServiceTypesRegisteredMessage"/> class.
    /// </summary>
    public NoServiceTypesRegisteredMessage()
        : base(1005, "NoServiceTypesRegistered", MessageSeverity.Error,
               "No service types registered", "NO_SERVICE_TYPES")
    { }

}
