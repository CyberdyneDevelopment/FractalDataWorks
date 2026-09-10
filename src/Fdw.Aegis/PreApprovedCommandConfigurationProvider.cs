using Fdw.Aegis.Abstractions;
using Fdw.Aegis.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Aegis;

/// <summary>Supplies the PreApproved implementation of an Aegis command.</summary>
public sealed class PreApprovedCommandConfigurationProvider
    : ImplementationProviderBase<PreApprovedCommandConfiguration, IApprovalPolicyConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="PreApprovedCommandConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public PreApprovedCommandConfigurationProvider(
        ILogger<PreApprovedCommandConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "aegis", "PreApprovedCommand")
    {
    }
}
