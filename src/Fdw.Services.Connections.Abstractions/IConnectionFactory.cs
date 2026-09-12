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
    // No members. Create(IGenericConfiguration) already comes from IServiceFactory<TConnection>, and
    // declaring it here too made every call on the generic interface ambiguous. What a connection
    // implementation needs resolved before it can be built is implementation-specific — a password,
    // a certificate, an opened workspace — so it is declared on that implementation's OWN factory
    // interface, where it can be typed honestly, and its provider is what fetches and passes it.

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
