using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Tags.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Tags;

/// <summary>Supplies the tag implementation's own configuration, read from <c>tag.TagConfiguration</c>.</summary>
public sealed class TagImplementationConfigurationProvider
    : ImplementationProviderBase<TagImplementationConfiguration, ITagImplementationConfiguration>,
      ITagImplementationConfigurationProvider
{
    /// <summary>Initializes the provider.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto <paramref name="dataStoreName"/>.</param>
    /// <param name="dataStoreName">The configuration connection the rows live on.</param>
    /// <param name="pathName">The schema the rows live in.</param>
    /// <param name="containerName">The implementation table.</param>
    public TagImplementationConfigurationProvider(
        ILogger<TagImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
