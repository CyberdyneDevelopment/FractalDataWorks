using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messages;

/// <summary>
/// CurrentMessage indicating that a service lifetime is null.
/// </summary>
[Message("LifetimeNull")]
public sealed class LifetimeNullMessage : FactoryMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LifetimeNullMessage"/> class.
    /// </summary>
    public LifetimeNullMessage()
        : base(3003, "LifetimeNull", MessageSeverity.Error,
               "Lifetime cannot be null", "FACTORY_LIFETIME_NULL")
    { }
}
