using Fdw.Abstractions;
using Fdw.Configuration;

namespace Fdw.Services.TokenManagers.Abstractions;

/// <summary>
/// Marker interface for token manager factories. Mirrors <c>ISchedulingFactory</c>: the non-generic
/// marker lets consumers reference "a token manager factory" without knowing TService/TConfiguration;
/// the generic form below is what concrete factories (and the closed <c>TokenManagerTypes</c>
/// ServiceTypeCollection) actually implement/close over.
/// </summary>
public interface ITokenManagerFactory
{
}

/// <summary>
/// Generic interface for token manager factories with typed configuration.
/// </summary>
/// <typeparam name="TService">The type of token manager this factory creates.</typeparam>
/// <typeparam name="TConfiguration">The type of configuration this factory requires.</typeparam>
public interface ITokenManagerFactory<TService, TConfiguration> : ITokenManagerFactory, IServiceFactory<TService, TConfiguration>
    where TService : ITokenManager
    where TConfiguration : IGenericConfiguration
{
}
