using System;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Identity.Authentik.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Identity.Authentik;

/// <summary>Reads and writes the <c>sec.AuthentikJwtFederationIdentity</c> typed body.</summary>
public class AuthentikJwtFederationConfigurationProvider
    : DefaultConfigurationProvider<AuthentikJwtFederationConfiguration, AuthentikJwtFederationConfigurationCommand>
{
    /// <summary>Initializes a new instance of the class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="lazyGateway">The configuration gateway.</param>
    /// <param name="dataStoreName">The store holding the table.</param>
    /// <param name="pathName">The schema the table lives in.</param>
    /// <param name="invalidator">Cache invalidator, when one is registered.</param>
    public AuthentikJwtFederationConfigurationProvider(
        ILogger<AuthentikJwtFederationConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "sec",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(logger ?? NullLogger<AuthentikJwtFederationConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName,
               invalidator)
    {
    }
}
