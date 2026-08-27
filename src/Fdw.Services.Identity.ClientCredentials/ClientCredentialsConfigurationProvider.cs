using System;
using Fdw.Services.Configuration;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Identity.ClientCredentials.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Identity.ClientCredentials;

/// <summary>
/// Reads and writes the <c>sec.ClientCredentialsIdentity</c> typed body.
/// </summary>
/// <remarks>
/// The header provider composes the aggregate by dispatching on ServiceOptionType to whichever
/// typed provider was registered for it, so a mechanism with no provider registered loads its
/// header and leaves Configuration null — which the factory reports as "typed configuration body
/// did not load", several layers from the missing registration.
/// </remarks>
public class ClientCredentialsConfigurationProvider
    : ImplementationConfigurationProvider<
          IIdentityServiceImplementationConfiguration,
          ClientCredentialsConfiguration,
          ClientCredentialsConfigurationCommand>
{
    /// <summary>Initializes a new instance of the class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="lazyGateway">The configuration gateway.</param>
    /// <param name="dataStoreName">The store holding the table.</param>
    /// <param name="pathName">The schema the table lives in.</param>
    public ClientCredentialsConfigurationProvider(
        ILogger<ClientCredentialsConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "sec")
        : base(logger ?? NullLogger<ClientCredentialsConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName)
    {
    }
}
