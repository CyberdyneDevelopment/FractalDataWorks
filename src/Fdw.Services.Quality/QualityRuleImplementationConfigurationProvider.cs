using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality;

/// <summary>Supplies the QualityRule implementation's own configuration.</summary>
public sealed class QualityRuleImplementationConfigurationProvider
    : ImplementationProviderBase<QualityRuleImplementationConfiguration, IQualityRuleImplementationConfiguration>,
      IQualityRuleImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="QualityRuleImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public QualityRuleImplementationConfigurationProvider(
        ILogger<QualityRuleImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "quality", "QualityRuleImplementation")
    {
    }
}
