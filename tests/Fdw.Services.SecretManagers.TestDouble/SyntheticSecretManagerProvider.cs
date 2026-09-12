using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;

namespace Fdw.Services.SecretManagers.TestDouble;

/// <summary>
/// Builds the synthetic secret manager used by tests.
/// </summary>
public sealed class SyntheticSecretManagerProvider
    : ImplementationServiceProviderBase<ISecretManager, ISecretManagerImplementationConfiguration>,
      IImplementationServiceProvider<ISecretManager, ISecretManagerImplementationConfiguration>
{
    private readonly ISyntheticSecretManagerFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="SyntheticSecretManagerProvider"/> class.</summary>
    /// <param name="factory">Builds the secret manager.</param>
    public SyntheticSecretManagerProvider(ISyntheticSecretManagerFactory factory) => _factory = factory;

    /// <inheritdoc />
    public override Task<IGenericResult<ISecretManager>> Create(
        ISecretManagerImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_factory.Create(configuration));
}
