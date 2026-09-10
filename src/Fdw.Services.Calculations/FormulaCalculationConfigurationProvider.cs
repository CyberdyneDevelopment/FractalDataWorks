using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Calculations;

/// <summary>Supplies the Formula calculation's own configuration.</summary>
public sealed class FormulaCalculationConfigurationProvider
    : ImplementationProviderBase<FormulaCalculationConfiguration, ICalculationEntityImplementationConfiguration>,
      IFormulaCalculationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="FormulaCalculationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public FormulaCalculationConfigurationProvider(
        ILogger<FormulaCalculationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "calc", "FormulaCalculation")
    {
    }
}
