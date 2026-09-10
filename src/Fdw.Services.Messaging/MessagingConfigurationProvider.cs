using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Messaging.Abstractions;
using Fdw.Services.Messaging.Commands;
using Fdw.Services.Messaging.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging;

/// <summary>Supplies the Messaging configuration.</summary>
public sealed class MessagingConfigurationProvider
    : ImplementationConfigurationProviderBase<IMessagingImplementationConfiguration>,
      IMessagingConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="MessagingConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public MessagingConfigurationProvider(
        ILogger<MessagingConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "msg", "Messaging")
    {
    }
}
