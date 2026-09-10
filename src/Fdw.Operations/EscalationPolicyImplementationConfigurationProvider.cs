using Fdw.Operations.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations;

/// <summary>Supplies the EscalationPolicy implementation's own configuration.</summary>
public sealed class EscalationPolicyImplementationConfigurationProvider
    : ImplementationProviderBase<EscalationPolicyImplementationConfiguration, IEscalationPolicyImplementationConfiguration>,
      IEscalationPolicyImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="EscalationPolicyImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public EscalationPolicyImplementationConfigurationProvider(
        ILogger<EscalationPolicyImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "workflow", "EscalationPolicyImplementation")
    {
    }
}
