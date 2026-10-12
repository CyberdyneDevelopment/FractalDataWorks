using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Results;

namespace Fdw.Services;

/// <summary>
/// The base every service factory derives from. It carries the one piece of logic a factory needs
/// that is not building the service: taking the configuration it was handed as the domain's
/// contract and turning it into the type this factory actually builds from.
/// </summary>
/// <typeparam name="TService">The service this factory creates.</typeparam>
/// <typeparam name="TConfiguration">The configuration this factory builds from.</typeparam>
/// <remarks>
/// A factory is reached through the domain, which knows only that it holds an implementation of
/// itself -- the marker -- so what arrives here is always widened. Every factory therefore needs
/// the same narrowing, and it is written once: a configuration of the wrong type is reported as a
/// result naming both types, never thrown, and never silently ignored.
/// </remarks>
public abstract class ServiceFactoryBase<TService, TConfiguration>
    : IServiceFactory<TService, TConfiguration>
    where TService : IGenericService
    where TConfiguration : IGenericConfiguration
{
    /// <summary>Creates the service from the configuration this factory builds from.</summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service, or a structured failure.</returns>
    public abstract IGenericResult<TService> Create(TConfiguration configuration);

    /// <inheritdoc />
    public IGenericResult<TService> Create(IGenericConfiguration configuration)
        => configuration is TConfiguration typed
            ? Create(typed)
            : GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("InvalidConfigurationType"),
                ResultDetails.Create(
                    "Factory", GetType().Name,
                    "ExpectedType", typeof(TConfiguration).Name,
                    "ActualType", configuration?.GetType().Name ?? "(null)"));

    // The erased surface the non-generic contract declares. Same one rule, widened return.
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        var created = Create(configuration);
        return created.IsSuccess && created.Value is not null
            ? GenericResult<IGenericService>.Success(created.Value)
            : created.ToNewResult<IGenericService>();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Two different failures share this one call, and each gets its own code: the configuration
    /// itself may be the wrong type (<c>InvalidConfigurationType</c>, same as the non-generic
    /// overload), or the configuration may be fine while the service it builds is not assignable to
    /// the caller's requested <typeparamref name="T"/> (<c>ServiceCastFailed</c>). Conflating them
    /// into one message would name the wrong thing as the problem in the second case.
    /// </remarks>
    public IGenericResult<T> Create<T>(IGenericConfiguration configuration)
        where T : IGenericService
    {
        var created = Create(configuration);
        if (created is { IsSuccess: true, Value: T typed })
            return GenericResult<T>.Success(typed);

        if (created.IsSuccess)
            return GenericResult<T>.Failure(
                ServicesResultCodes.ByName("ServiceCastFailed"),
                ResultDetails.Create(
                    "Factory", GetType().Name,
                    "ExpectedType", typeof(T).Name,
                    "ActualType", typeof(TService).Name));

        return created.ToNewResult<T>();
    }
}
