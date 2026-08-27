using System;
using Fdw.Services.Abstractions;
using Fdw.Services.Identity.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Identity;

/// <summary>
/// Resolves identity services by configuration name or id.
/// </summary>
public sealed class IdentityServiceProvider
    : PlatformServiceProviderBase<
          IIdentityService,
          IIdentityServiceImplementationConfiguration,
          IIdentityServiceFactory<IIdentityService, IIdentityServiceImplementationConfiguration>,
          IIdentityServiceConfigurationProvider>,
      IIdentityServiceProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityServiceProvider"/> class.
    /// </summary>
    /// <param name="services">The container this provider resolves factories from.</param>
    /// <param name="logger">The logger instance.</param>
    public IdentityServiceProvider(
        IServiceProvider services,
        ILogger<IdentityServiceProvider> logger)
        : base(services, logger ?? NullLogger<IdentityServiceProvider>.Instance)
    {
    }
}
