using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messages;

/// <summary>
/// CurrentMessage indicating that a factory instance is null.
/// </summary>
[Message("FactoryNull")]
public sealed class FactoryNullMessage : FactoryMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FactoryNullMessage"/> class.
    /// </summary>
    public FactoryNullMessage()
        : base(3002, "FactoryNull", MessageSeverity.Error,
               "Factory cannot be null", "FACTORY_NULL")
    { }
}
