using System;
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
    : DomainConfigurationProviderBase<ICalculationEntityImplementationConfiguration, CalculationEntityDomainConfiguration>,
      ICalculationConfigurationProvider,
      IPlacedConfigurationProvider<ICalculationEntityImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="CalculationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="placement">Places each member this provider makes, in the unit of work that makes it (FDW-3692).</param>
    public CalculationConfigurationProvider(
        ILogger<CalculationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName,
        IConfigurationPlacement placement)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
        Placement = placement;
    }

    /// <inheritdoc />
    public IConfigurationPlacement Placement { get; }

    /// <inheritdoc />
    public string PlacementKind => "Calculation";

    /// <inheritdoc />
    public bool Needs(ICalculationEntityImplementationConfiguration record) => true;
}
