using System;
using Fdw.Commands.Data;
using Fdw.Configuration;
using Fdw.Conventions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Commands;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>Supplies the DataSet configuration.</summary>
public sealed class DataSetConfigurationProvider
    : DomainConfigurationProviderBase<IDataSetImplementationConfiguration>,
      IDataSetConfigurationProvider,
      IPlacedConfigurationProvider<IDataSetImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataSetConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="placement">Places each data set this provider makes, in the unit of work that makes it (FDW-3692).</param>
    public DataSetConfigurationProvider(
        ILogger<DataSetConfigurationProvider> logger,
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
    public string PlacementKind => "DataSet";

    /// <inheritdoc />
    /// <remarks>
    /// A variation is a data set whose own row names its owner (a workspace for a scoped variation, a user for a data view), and the workspace read
    /// already lists the variations a workspace owns, so a placement row would list it twice or give a data view a workspace it does not have.
    /// Every other data set is placed.
    /// </remarks>
    public bool Needs(IDataSetImplementationConfiguration record) => record is not IDataSetVariationImplementationConfiguration;
}
