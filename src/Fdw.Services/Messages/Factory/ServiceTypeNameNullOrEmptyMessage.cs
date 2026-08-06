using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messages;

/// <summary>
/// CurrentMessage indicating that a service type name is null or empty.
/// </summary>
[Message("ServiceTypeNameNullOrEmpty")]
public sealed class ServiceTypeNameNullOrEmptyMessage : FactoryMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceTypeNameNullOrEmptyMessage"/> class.
    /// </summary>
    public ServiceTypeNameNullOrEmptyMessage()
        : base(3001, "ServiceTypeNameNullOrEmpty", MessageSeverity.Error,
               "Service type name cannot be null or empty", "FACTORY_TYPE_NAME_NULL")
    { }
}
