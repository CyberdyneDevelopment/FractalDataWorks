using System;
using Fdw.Services.Configuration;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Identity.JwtAssertion.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Identity.JwtAssertion;

/// <summary>Reads and writes the <c>sec.JwtAssertionIdentity</c> typed body.</summary>
public class JwtAssertionConfigurationProvider
    : ImplementationConfigurationProvider<
          IIdentityServiceImplementationConfiguration,
          JwtAssertionConfiguration,
          JwtAssertionConfigurationCommand>
{
    /// <summary>Initializes a new instance of the class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="lazyGateway">The configuration gateway.</param>
    /// <param name="dataStoreName">The store holding the table.</param>
    /// <param name="pathName">The schema the table lives in.</param>
    public JwtAssertionConfigurationProvider(
        ILogger<JwtAssertionConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "sec")
        : base(logger ?? NullLogger<JwtAssertionConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName)
    {
    }
}
