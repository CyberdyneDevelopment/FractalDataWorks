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
    : DomainServiceProviderBase<
          IIdentityService,
          IIdentityServiceImplementationConfiguration,
          IIdentityServiceFactory<IIdentityService, IIdentityServiceImplementationConfiguration>,
          IIdentityServiceConfigurationProvider>,
      IIdentityServiceProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityServiceProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public IdentityServiceProvider(
        ILogger<IdentityServiceProvider> logger)
        : base(logger ?? NullLogger<IdentityServiceProvider>.Instance)
    {
    }
}
