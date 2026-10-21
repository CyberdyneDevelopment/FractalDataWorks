using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>Supplies the Glossary implementation of Category: a category for organising glossary terms.</summary>
/// <remarks>It is registered into <see cref="CategoryConfigurationProvider"/> under the name Glossary.</remarks>
public sealed class GlossaryCategoryConfigurationProvider
    : ImplementationProviderBase<GlossaryCategoryConfiguration, ICategoryImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GlossaryCategoryConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public GlossaryCategoryConfigurationProvider(
        ILogger<GlossaryCategoryConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
