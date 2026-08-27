using System;
using Fdw.Services;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders;

/// <summary>
/// Resolves external identity providers by configuration name or id.
/// </summary>
public sealed class ExternalIdentityProviderServiceProvider
    : PlatformServiceProviderBase<
          IExternalIdentityProvider,
          IExternalIdentityProviderImplementationConfiguration,
          IExternalIdentityProviderFactory<IExternalIdentityProvider, IExternalIdentityProviderImplementationConfiguration>,
          IExternalIdentityProviderConfigurationProvider>,
      IExternalIdentityProviderServiceProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalIdentityProviderServiceProvider"/> class.
    /// </summary>
    /// <param name="services">The container this provider resolves factories from.</param>
    /// <param name="logger">The logger instance.</param>
    public ExternalIdentityProviderServiceProvider(
        IServiceProvider services,
        ILogger<ExternalIdentityProviderServiceProvider> logger)
        : base(services, logger ?? NullLogger<ExternalIdentityProviderServiceProvider>.Instance)
    {
    }
}
