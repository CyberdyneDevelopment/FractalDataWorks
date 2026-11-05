using System;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Commands;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Commands;
using Fdw.Services.Data.Configuration;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>Supplies the DataStore configuration.</summary>
public sealed class DataStoreConfigurationProvider
    : DomainConfigurationProviderBase<IDataStoreImplementationConfiguration, DataStoreDomainConfiguration>,
      IPlacedConfigurationProvider<IDataStoreImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataStoreConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="placement">Places each member this provider makes, in the unit of work that makes it (FDW-3692).</param>
    public DataStoreConfigurationProvider(
        ILogger<DataStoreConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName,
        IConfigurationPlacement placement)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
        Placement = placement ?? throw new ArgumentNullException(nameof(placement));
    }

    /// <inheritdoc />
    public IConfigurationPlacement Placement { get; }

    /// <inheritdoc />
    public string PlacementKind => "DataStore";

    /// <inheritdoc />
    public bool Needs(IDataStoreImplementationConfiguration record) => true;
}
