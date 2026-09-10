using Fdw.Commands.Data;
using Fdw.Configuration;
using Fdw.Conventions;
using Fdw.Data.DataSets;
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
    : ImplementationConfigurationProviderBase<IDataSetImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataSetConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public DataSetConfigurationProvider(
        ILogger<DataSetConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "data", "DataSet")
    {
    }
}
