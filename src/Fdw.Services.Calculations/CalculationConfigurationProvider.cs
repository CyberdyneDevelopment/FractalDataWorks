using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Commands;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Calculations;

/// <summary>Supplies the CalculationEntity configuration.</summary>
public sealed class CalculationConfigurationProvider
    : DomainConfigurationProviderBase<ICalculationEntityImplementationConfiguration>,
      ICalculationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="CalculationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public CalculationConfigurationProvider(
        ILogger<CalculationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "calc", "CalculationEntity")
    {
    }
}
