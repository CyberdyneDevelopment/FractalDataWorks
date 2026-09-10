using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality;

/// <summary>Supplies the configured QualityRule members.</summary>
public sealed class QualityRuleConfigurationProvider
    : DomainConfigurationProviderBase<IQualityRuleImplementationConfiguration>,
      IQualityRuleConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="QualityRuleConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public QualityRuleConfigurationProvider(
        ILogger<QualityRuleConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "quality", "QualityRule")
    {
    }
}
