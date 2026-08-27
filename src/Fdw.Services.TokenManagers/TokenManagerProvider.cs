using System;
using Fdw.Services.Abstractions;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// Resolves token managers by configuration name or id.
/// </summary>
public sealed class TokenManagerProvider
    : PlatformServiceProviderBase<
          ITokenManager,
          ITokenManagerImplementationConfiguration,
          ITokenManagerFactory<ITokenManager, ITokenManagerImplementationConfiguration>,
          ITokenManagerConfigurationProvider>,
      ITokenManagerProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenManagerProvider"/> class.
    /// </summary>
    /// <param name="services">The container this provider resolves factories from.</param>
    /// <param name="logger">The logger instance.</param>
    public TokenManagerProvider(
        IServiceProvider services,
        ILogger<TokenManagerProvider> logger)
        : base(services, logger ?? NullLogger<TokenManagerProvider>.Instance)
    {
    }
}
