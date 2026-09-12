using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// The implementation provider for the connection a configuration gateway opens.
/// </summary>
/// <remarks>
/// Why this exists rather than resolving the connection's own implementation provider: this is the
/// connection every other connection's configuration is read through, so anything it depends on
/// must already exist before any configuration has been read. An implementation provider resolved
/// from the container brings its whole dependency graph with it — a secret-manager provider, a
/// logger — and those reach the logging domain, which reads its configuration back through this
/// gateway. That cycle does not throw; it parks the host with no exception and no log line.
/// <para>
/// It is also unnecessary. The connections a gateway opens declare no authentication and no secret
/// — <c>configurationSchema.json</c> ships them as <c>AuthenticationType: None</c> — and the code
/// has always said this caller cannot resolve a secret manager by name anyway, because the provider
/// that would do so reads its own configuration out of the store being opened. So there is nothing
/// to resolve here, and a provider that only builds is the honest shape.
/// </para>
/// <para>
/// A connection that DID declare a secret would fail loud in the factory rather than silently
/// building without one, which is the correct outcome for a store that cannot reach a secret.
/// </para>
/// </remarks>
public sealed class ConfigurationConnectionProvider
    : ImplementationServiceProviderBase<IGenericConnection, IConnectionImplementationConfiguration>
{
    private readonly IConnectionFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationConnectionProvider"/> class.
    /// </summary>
    /// <param name="factory">The factory registered for the connection's implementation.</param>
    public ConfigurationConnectionProvider(IConnectionFactory factory) => _factory = factory;

    /// <inheritdoc />
    public override Task<IGenericResult<IGenericConnection>> Create(
        IConnectionImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
        => Task.FromResult(((IServiceFactory<IGenericConnection>)_factory).Create(configuration));
}
