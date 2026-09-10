using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Calculations;

/// <summary>Supplies the Windowed calculation's own configuration.</summary>
public sealed class WindowedCalculationConfigurationProvider
    : ImplementationProviderBase<WindowedCalculationConfiguration, ICalculationEntityImplementationConfiguration>,
      IWindowedCalculationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="WindowedCalculationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public WindowedCalculationConfigurationProvider(
        ILogger<WindowedCalculationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "calc", "WindowedCalculation")
    {
    }
}
