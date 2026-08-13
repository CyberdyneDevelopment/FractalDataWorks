using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Fdw.Results;

namespace Fdw.Services.Messaging;

/// <summary>
/// Default messaging service type that registers <see cref="IMessageService"/>
/// and <see cref="IAccessRequestService"/> with the dependency injection container.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(MessagingServiceTypes), "Default")]
public sealed class DefaultMessagingServiceType : MessagingServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMessagingServiceType"/> class.
    /// </summary>
    public DefaultMessagingServiceType()
        : base(
            "Default",
            "Messaging:Default",
            "Default Messaging",
            "Default messaging services with message and access request support")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddScoped<IMessageService, MessageService>();
            builder.Services.AddScoped<IAccessRequestService, AccessRequestService>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
