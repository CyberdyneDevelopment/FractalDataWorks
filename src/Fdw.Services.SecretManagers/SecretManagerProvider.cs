using Microsoft.Extensions.Logging;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;

namespace Fdw.Services.SecretManagers;

/// <summary>
/// The SecretManagers domain provider. Behaviourally identical to
/// <see cref="PlatformServiceProviderBase{TService, TConfiguration, TFactory, TConfigurationProvider}"/> —
/// it exists so the domain's own <see cref="ISecretManagerProvider"/> interface has a concrete type
/// behind it.
/// </summary>
/// <remarks>
/// Why: consumers that need a secret manager BY NAME (connection factories, most of all) take
/// <see cref="ISecretManagerProvider"/> as a constructor dependency. Without a concrete type that
/// implements it, the only injectable shape is the raw <c>IPlatformServiceProvider&lt;,&gt;</c>, which
/// FDW045 forbids in a factory constructor. Mirrors <c>ConnectionProvider</c>, which is the
/// same one-line specialisation for the Connections domain.
/// </remarks>
public sealed class SecretManagerProvider
    : PlatformServiceProviderBase<
          ISecretManager,
          ISecretManagerImplementationConfiguration,
          ISecretManagerServiceFactory<ISecretManager, ISecretManagerImplementationConfiguration>,
          ISecretManagerConfigurationProvider>,
      ISecretManagerProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagerProvider"/> class.
    /// </summary>
    /// <param name="services">The container this provider resolves factories from.</param>
    /// <param name="logger">The logger instance.</param>
    public SecretManagerProvider(IServiceProvider services, ILogger<SecretManagerProvider> logger)
        : base(services, logger)
    {
    }
}
