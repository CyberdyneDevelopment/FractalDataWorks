using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Non-generic connection factory interface. Used by <c>IConfigurationGateway</c>
/// and any consumer that does not know the concrete connection configuration type at compile time.
/// </summary>
public interface IConnectionFactory
{
    /// <summary>
    /// Creates a connection from a configuration whose authentication type needs no secret.
    /// </summary>
    /// <param name="configuration">The connection configuration. Must be castable to the factory's concrete configuration type.</param>
    /// <returns>
    /// The created connection, or a structured failure. A configuration whose authentication type
    /// declares a secret fails loud here — this overload resolves nothing. Ask
    /// <see cref="RequiredSecret"/> first and use the overload that takes the fetched value.
    /// </returns>
    IGenericResult<IGenericConnection> Create(IGenericConfiguration configuration);

    /// <summary>
    /// Reports what this configuration's authentication type needs fetched before a connection can
    /// be built.
    /// </summary>
    /// <param name="configuration">The connection configuration (composed header or typed body).</param>
    /// <returns>
    /// The manager and key to read, or <see cref="SecretRequirement.None"/> when the authentication
    /// type needs no secret. A configuration that declares a key without a manager — or names an
    /// authentication type that does not resolve — is a defect, and fails here rather than later.
    /// </returns>
    /// <remarks>
    /// The factory does not answer this itself; it asks the connection's authentication type, which
    /// owns <c>SecretManagerName</c> and <c>SecretKeyName</c>. The factory only relays, so a new
    /// authentication type changes nothing here.
    /// </remarks>
    IGenericResult<SecretRequirement> RequiredSecret(IGenericConfiguration configuration);

    /// <summary>
    /// Creates a connection from a configuration whose secret has already been fetched.
    /// </summary>
    /// <param name="configuration">The connection configuration (composed header or typed body).</param>
    /// <param name="resolvedSecret">
    /// The value read for the <see cref="RequiredSecret"/> this configuration reported, or
    /// <see langword="null"/> when it reported none.
    /// </param>
    /// <remarks>
    /// Why this takes the value rather than a secret manager: fetching is resolution, and resolution
    /// belongs to the provider that holds <c>ISecretManagerProvider</c>. The factory builds from what
    /// it is handed, which is what keeps every <c>Create</c> synchronous. The authentication type
    /// still builds the secret portion of the connection string — it is handed this value and
    /// produces the fragment.
    /// </remarks>
    IGenericResult<IGenericConnection> Create(IGenericConfiguration configuration, string? resolvedSecret);
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
