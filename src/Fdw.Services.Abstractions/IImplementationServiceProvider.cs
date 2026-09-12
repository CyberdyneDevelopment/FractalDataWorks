using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Services.Abstractions;

/// <summary>
/// The erased surface of an implementation service provider: what a caller holding no type argument
/// can ask.
/// </summary>
public interface IImplementationServiceProvider
{
    /// <summary>Builds the service this implementation provides, from its implementation configuration.</summary>
    /// <param name="configuration">The implementation configuration the domain provider read and dispatched.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The service, or a structured failure.</returns>
    Task<IGenericResult<global::Fdw.Abstractions.IGenericService>> Create(
        IImplementationConfiguration configuration,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Builds the service for exactly one implementation of a domain.
/// </summary>
/// <typeparam name="TService">The service this domain provides.</typeparam>
/// <typeparam name="TConfiguration">The domain's implementation configuration contract.</typeparam>
/// <remarks>
/// The service-side twin of <see cref="IImplementationConfigurationProvider{TConfiguration}"/>, and
/// it divides the same way: a domain provider holds the registry and dispatches, an implementation
/// provider does the work for one implementation and holds no registry, no dispatch and no
/// <c>Register</c>.
/// <para>
/// This is where anything that must be RESOLVED before the service can be built belongs — a secret
/// read out of a manager, a handle opened. An implementation provider may know its own
/// implementation, so it can ask that implementation's own types what they need; a domain provider
/// may not, which is why the resolution cannot live there. The factory below it stays synchronous
/// and builds from what it is handed.
/// </para>
/// <para>
/// Close this over the domain's implementation <i>contract</i>, never over a single implementation's
/// concrete class, so one constraint accepts every provider a domain has.
/// </para>
/// </remarks>
public interface IImplementationServiceProvider<TService, TConfiguration> : IImplementationServiceProvider
    where TService : global::Fdw.Abstractions.IGenericService
    where TConfiguration : IImplementationConfiguration
{
    /// <summary>Builds the service from this implementation's configuration.</summary>
    /// <param name="configuration">The implementation configuration the domain provider dispatched.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The service, or a structured failure.</returns>
    Task<IGenericResult<TService>> Create(
        TConfiguration configuration,
        CancellationToken cancellationToken = default);
}
