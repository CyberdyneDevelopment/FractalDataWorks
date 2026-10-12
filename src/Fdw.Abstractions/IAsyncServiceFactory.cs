using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Abstractions;

/// <summary>
/// A factory whose creation needs to await something — resolving a secret, opening a handle.
/// </summary>
/// <typeparam name="TService">The service this factory builds.</typeparam>
/// <remarks>
/// A provider prefers this overload when the registered factory offers it, so a domain whose creation
/// is genuinely asynchronous needs no provider of its own to reach it.
/// </remarks>
public interface IAsyncServiceFactory<TService> : IServiceFactory<TService>
    where TService : IGenericService
{
    /// <summary>Creates the service.</summary>
    /// <param name="configuration">The implementation configuration to build from.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created service, or a structured failure.</returns>
    Task<IGenericResult<TService>> Create(IGenericConfiguration configuration, CancellationToken cancellationToken = default);
}
