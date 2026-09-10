using Fdw.Commands.Data;
using Fdw.Operations.Commands;
using Fdw.Operations.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations;

/// <summary>Supplies the EscalationPolicy configuration.</summary>
public sealed class EscalationConfigurationProvider
    : ImplementationConfigurationProviderBase<IEscalationPolicyImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="EscalationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public EscalationConfigurationProvider(
        ILogger<EscalationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "workflow", "EscalationPolicy")
    {
    }
}
