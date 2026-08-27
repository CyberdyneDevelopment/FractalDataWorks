using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Non-generic connection factory interface. Used by <c>IConfigurationGateway</c>
/// and any consumer that does not know the concrete connection configuration type at compile time.
/// </summary>
public interface IConnectionFactory
{
    /// <summary>
    /// Creates a connection from a generic configuration, WITHOUT resolving any secret.
    /// </summary>
    /// <param name="configuration">The connection configuration. Must be castable to the factory's concrete configuration type.</param>
    /// <returns>
    /// The created connection, or a structured failure. If the connection's authentication configuration
    /// requires a secret, this overload FAILS LOUD (it never resolves secrets) — the caller must use one
    /// of the async overloads that supply a secret source.
    /// </returns>
    // Why: this is the pure-construction contract (IServiceFactory.Create). Secret resolution is async,
    // so it cannot happen here at all — a connection whose authentication type requires a secret must be
    // built through one of the async overloads below.
    IGenericResult<IGenericConnection> Create(IGenericConfiguration configuration);

    /// <summary>
    /// Creates a connection asynchronously using an already-resolved <paramref name="secretManager"/>.
    /// Used by <c>ConfigurationGateway</c> during config-DB bootstrap, where the caller holds a specific
    /// secret manager (env-var backed) and the FDW secret-manager provider is not yet available.
    /// </summary>
    /// <param name="configuration">The connection configuration. Must be castable to the factory's concrete configuration type.</param>
    /// <param name="secretManager">The secret manager to use for any secret/password resolution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult<IGenericConnection>> Create(
        IGenericConfiguration configuration,
        ISecretManager? secretManager,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a connection asynchronously, resolving its secret manager BY NAME from the connection's
    /// own authentication configuration through the secret-manager provider the factory was CONSTRUCTED
    /// with. This is the runtime path used by <c>ConnectionProvider</c>.
    /// </summary>
    /// <param name="configuration">The connection configuration (composed header or typed body).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// The secret-manager provider is a constructor dependency of the factory — each connection type's
    /// <c>Register</c> supplies it, exactly as the reference <c>HttpConnectionType</c>
    /// (<c>ReferenceConnections.Http.ServiceType</c>, outside this repository) supplies
    /// <c>IHttpClientFactory</c>. Nothing outside the factory decides whether a secret is needed; the
    /// connection's authentication type does, through the properties it declares as secret-bearing.
    /// </remarks>
    Task<IGenericResult<IGenericConnection>> Create(
        IGenericConfiguration configuration,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic interface for connection factories with typed configuration.
/// </summary>
/// <typeparam name="TConnection">The type of connection this factory creates.</typeparam>
/// <typeparam name="TConfiguration">The type of configuration this factory requires.</typeparam>
public interface IConnectionFactory<TConnection, TConfiguration> : IConnectionFactory, IServiceFactory<TConnection, TConfiguration>
    where TConnection : IGenericConnection
    where TConfiguration : IGenericConfiguration
{
}
