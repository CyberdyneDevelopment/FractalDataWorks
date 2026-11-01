using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Results;

namespace Fdw.Services;

/// <summary>
/// The base every implementation service provider derives from. It carries the one piece of logic
/// such a provider needs that is not building the service: taking the configuration the domain
/// dispatched as the domain's contract and turning it into the type this implementation builds from.
/// </summary>
/// <typeparam name="TService">The service this domain provides.</typeparam>
/// <typeparam name="TConfiguration">The implementation configuration this provider builds from.</typeparam>
/// <remarks>
/// The service-side twin of <c>ImplementationProviderBase</c>, and it divides the same way: the
/// domain provider holds the registry and dispatches; this holds none and handles one implementation.
/// <para>
/// A derived provider injects whatever that implementation needs — a secret-manager provider, an
/// HTTP client factory, its own named factory interface — and resolves those here, before calling a
/// factory that is synchronous. That is the whole reason this layer exists: resolution needs to
/// await and needs to know the implementation, and the domain provider may do neither.
/// </para>
/// </remarks>
public abstract class ImplementationServiceProviderBase<TService, TConfiguration>
    : IImplementationServiceProvider<TService, TConfiguration>
    where TService : IGenericService
    where TConfiguration : IImplementationConfiguration
{
    /// <summary>Builds the service from this implementation's configuration.</summary>
    /// <param name="configuration">The implementation configuration the domain provider dispatched.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The service, or a structured failure.</returns>
    public abstract Task<IGenericResult<TService>> Create(
        TConfiguration configuration,
        CancellationToken cancellationToken = default);

    // The erased surface the non-generic contract declares. A configuration of the wrong type is
    // reported as a result naming both types, never thrown, and never silently ignored -- the same
    // one rule ServiceFactoryBase applies, applied once here so no implementation repeats it.
    async Task<IGenericResult<IGenericService>> IImplementationServiceProvider.Create(
        IImplementationConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (configuration is not TConfiguration typed)
            return GenericResult<IGenericService>.Failure(
                ServicesResultCodes.ByName("InvalidConfigurationType"),
                ResultDetails.Create(
                    "Provider", GetType().Name,
                    "ExpectedType", typeof(TConfiguration).Name,
                    "ActualType", configuration?.GetType().Name ?? "(null)"));

        var created = await Create(typed, cancellationToken).ConfigureAwait(false);
        return created.IsSuccess && created.Value is not null
            ? GenericResult<IGenericService>.Success(created.Value)
            : created.ToNewResult<IGenericService>();
    }
}
