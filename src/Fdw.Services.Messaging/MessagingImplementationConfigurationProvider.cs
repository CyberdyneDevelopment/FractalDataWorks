using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Messaging.Configuration;
using Fdw.Services.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging;

/// <summary>Supplies the Messaging implementation's own configuration.</summary>
public sealed class MessagingImplementationConfigurationProvider
    : ImplementationProviderBase<MessagingImplementationConfiguration, IMessagingImplementationConfiguration>,
      IMessagingImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="MessagingImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public MessagingImplementationConfigurationProvider(
        ILogger<MessagingImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "msg", "MessagingImplementation")
    {
    }
}
