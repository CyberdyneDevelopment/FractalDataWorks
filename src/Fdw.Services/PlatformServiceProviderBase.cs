using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Logging;
using Fdw.Services.Results;
using Fdw.ServiceTypes;

namespace Fdw.Services;

/// <summary>
/// The one implementation of the platform provider contract. A domain derives a named, closed
/// subclass from it — <c>ConnectionProvider</c>, <c>NotificationProvider</c> — and nothing closes
/// this type at a use site.
/// </summary>
/// <typeparam name="TService">The service this provider resolves.</typeparam>
/// <typeparam name="TConfiguration">The configuration that service binds to.</typeparam>
/// <typeparam name="TFactory">The factory that builds the service.</typeparam>
/// <typeparam name="TConfigurationProvider">The provider that supplies the typed configuration.</typeparam>
public abstract class PlatformServiceProviderBase<TService, TConfiguration, TFactory, TConfigurationProvider>
    : IPlatformServiceProvider<TService, TConfiguration, TFactory, TConfigurationProvider>
    where TService : IGenericService
    where TConfiguration : class, IImplementationConfiguration
    where TFactory : IServiceFactory<TService>
    where TConfigurationProvider : IDomainConfigurationProvider<TConfiguration>
{
    private readonly ILogger<PlatformServiceProviderBase<TService, TConfiguration, TFactory, TConfigurationProvider>> _logger;
    private readonly Dictionary<string, IServiceFactory<TService>> _factories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IServiceConfigurationProvider> _configurationProviders = new(StringComparer.OrdinalIgnoreCase);
    private IDomainConfigurationProvider<TConfiguration>? _domainConfigurationProvider;

    /// <summary>Gets the registered service factories keyed by service option type.</summary>
    protected IDictionary<string, IServiceFactory<TService>> Factories => _factories;

    /// <summary>Gets the domain's parent configuration provider.</summary>
    // Why the erased view: a configuration provider is always closed over its CONCRETE configuration
    // class and IServiceConfigurationProvider<T> is invariant, so no single closed typed field can hold
    // one. This base reads only Id and ServiceOptionType off the record it returns.
    protected IDomainConfigurationProvider<TConfiguration>? DomainConfigurationProvider => _domainConfigurationProvider;

    private static readonly Dictionary<string, Func<IServiceProvider, IServiceFactory<TService>>> _registered
        = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, IServiceConfigurationProvider> _registeredConfigurationProviders
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers the factory for one service option type. Called from that option's Register method.
    /// </summary>
    /// <param name="serviceOptionType">The option's discriminator.</param>
    /// <param name="factory">Resolves the factory once the container exists.</param>
    public static void Register(string serviceOptionType, Func<IServiceProvider, IServiceFactory<TService>> factory)
    {
        if (string.IsNullOrEmpty(serviceOptionType))
            throw new ArgumentNullException(nameof(serviceOptionType));

        _registered[serviceOptionType] = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="PlatformServiceProviderBase{TService, TConfiguration, TFactory, TConfigurationProvider}"/> class.
    /// </summary>
    /// <param name="services">The scope's container, used to resolve the registered factories.</param>
    /// <param name="logger">The logger for this provider.</param>
    // Why the container is resolved here and NOT stored: every registered func is invoked once, now,
    // against this scope. The provider keeps the resulting factories, never the container — so
    // nothing can reach back into DI at request time.
    protected PlatformServiceProviderBase(
        IServiceProvider services,
        ILogger<PlatformServiceProviderBase<TService, TConfiguration, TFactory, TConfigurationProvider>> logger)
    {
        _logger = logger;

        if (services is null)
        {
            ServiceLogger.ContainerNotSupplied(_logger, GetType().Name);
            return;
        }

        var providerType = GetType().Name;

        foreach (var registration in _registered)
        {
            var factory = registration.Value(services);
            _factories[registration.Key] = factory;
            ServiceLogger.ProviderFactoryRegistered(_logger, registration.Key);
            ServiceLogger.FactoryResolvedIntoProvider(
                _logger, providerType, factory?.GetType().Name ?? "<null>", registration.Key);
        }

        ServiceLogger.ProviderFactoryRegistryDrained(
            _logger, providerType, _factories.Count, string.Join(", ", _factories.Keys));

        if (_factories.Count == 0)
        {
            ServiceLogger.ProviderFactoryRegistryEmpty(_logger, providerType);
        }
        else
        {
            ServiceLogger.ProviderReady(
                _logger, providerType, _factories.Count, string.Join(", ", _factories.Keys));
        }

        foreach (var registration in _registeredConfigurationProviders)
        {
            _configurationProviders[registration.Key] = registration.Value;
            ServiceLogger.ProviderConfigurationRegistered(_logger, registration.Key);
        }
    }

    // ── Registration ────────────────────────────────────────────────────────

    // Why an adapter rather than a checked cast: IServiceConfigurationProvider<T> does NOT inherit the
    // erased IServiceConfigurationProvider — they are two independent interfaces that
    // ImplementationConfigurationProviderBase happens to implement both of. Register accepts the typed one in its
    // signature, so refusing an argument that satisfies that signature makes the signature a lie, and
    // the refusal lands at start-up on something the compiler already accepted. Erasing it here means
    // every value the signature admits actually works.
    private static IServiceConfigurationProvider Erase<TConcrete>(IServiceConfigurationProvider<TConcrete> provider)
        where TConcrete : class, TConfiguration
        => provider as IServiceConfigurationProvider ?? new ErasedConfigurationProvider<TConcrete>(provider);

    private sealed class ErasedConfigurationProvider<TConcrete> : IServiceConfigurationProvider
        where TConcrete : class, TConfiguration
    {
        private readonly IServiceConfigurationProvider<TConcrete> _inner;

        public ErasedConfigurationProvider(IServiceConfigurationProvider<TConcrete> inner) => _inner = inner;

        public async Task<IGenericResult<IGenericConfiguration>> Get(Guid id, CancellationToken ct = default)
            => Widen(await _inner.Get(id, ct).ConfigureAwait(false));

        public async Task<IGenericResult<IGenericConfiguration>> Get(string name, CancellationToken ct = default)
            => Widen(await _inner.Get(name, ct).ConfigureAwait(false));

        public async Task<IGenericResult> Save(IGenericConfiguration record, CancellationToken ct = default)
        {
            if (record is not TConcrete typed)
            {
                return GenericResult.Failure(
                    ServicesResultCodes.ByName("InvalidConfigurationType"),
                    ResultDetails.Create("ExpectedType", typeof(TConcrete).Name,
                                         "ActualType", record?.GetType().Name ?? "(null)"));
            }

            return await _inner.Save(typed, ct).ConfigureAwait(false);
        }

        public Task<IGenericResult> Delete(Guid id, CancellationToken ct = default) => _inner.Delete(id, ct);

        private static IGenericResult<IGenericConfiguration> Widen(IGenericResult<TConcrete> result)
            => result.IsSuccess && result.Value is not null
                ? GenericResult<IGenericConfiguration>.Success(result.Value)
                : result.ToNewResult<IGenericConfiguration>();
    }

    /// <inheritdoc />
    public IGenericResult Register(string serviceOptionType, IServiceFactory<TService> factory)
    {
        _factories[serviceOptionType] = factory;
        _registered[serviceOptionType] = _ => factory;
        ServiceLogger.ProviderFactoryRegistered(_logger, serviceOptionType);
        return GenericResult.Success();
    }
    /// <inheritdoc />
    public IGenericResult Register(IDomainConfigurationProvider<TConfiguration> domainConfigurationProvider)
    {
        _domainConfigurationProvider = domainConfigurationProvider;
        ServiceLogger.DomainConfigurationProviderRegistered(_logger);
        return GenericResult.Success();
    }

    /// <summary>
    /// Invokes the registered factory to build the service. Override to supply additional
    /// already-resolved dependencies to a domain-specific <c>Create</c> overload.
    /// </summary>
    /// <param name="factory">The factory registered for the configuration's ServiceOptionType.</param>
    /// <param name="configuration">The resolved (composed) configuration.</param>
    /// <returns>The created service, or a structured failure.</returns>
    private static IGenericResult<TService> Create(IServiceFactory<TService> factory, TConfiguration configuration)
        => factory.Create(configuration);

    // ── Resolution ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    public virtual async Task<IGenericResult<TService>> Get(string name, CancellationToken cancellationToken = default)
        => await Resolve(name, ct => _domainConfigurationProvider!.Get(name, ct), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public virtual async Task<IGenericResult<TService>> Get(Guid id, CancellationToken cancellationToken = default)
        => await Resolve(id.ToString(), ct => _domainConfigurationProvider!.Get(id, ct), cancellationToken).ConfigureAwait(false);

    // Why one path for both: the domain configuration provider does the whole resolution — it finds the
    // member, reads the ServiceOptionType that member names, and returns the implementation
    // configuration. All that is left here is choosing the factory registered for that same type.
    private async Task<IGenericResult<TService>> Resolve(
        string identifier,
        Func<CancellationToken, Task<IGenericResult<TConfiguration>>> get,
        CancellationToken cancellationToken)
    {
        ServiceLogger.GettingServiceByName(_logger, identifier);

        if (_domainConfigurationProvider is null)
        {
            ServiceLogger.NoDomainConfigurationProviderRegistered(_logger, identifier);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("NoDomainConfigurationProvider"),
                ResultDetails.Create("Identifier", identifier));
        }

        var configuration = await get(cancellationToken).ConfigureAwait(false);
        if (!configuration.IsSuccess || configuration.Value is null)
        {
            ServiceLogger.ConfigurationNotFound(_logger, identifier);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("ConfigurationNotFound"),
                ResultDetails.Create("Identifier", identifier));
        }

        return await CreateFrom(configuration.Value, identifier, cancellationToken).ConfigureAwait(false);
    }

    // Why the registry and not the container: each option registered its factory func, and this
    // provider resolved every one of them in its constructor. Nothing reaches back into DI at
    // request time.
    private async Task<IGenericResult<TService>> CreateFrom(
        TConfiguration configuration, string identifier, CancellationToken cancellationToken)
    {
        var serviceOptionType = configuration.ServiceOptionType;
        if (string.IsNullOrEmpty(serviceOptionType))
        {
            ServiceLogger.ServiceOptionTypeMissing(_logger, identifier);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("ServiceOptionTypeMissing"),
                ResultDetails.Create("Identifier", identifier));
        }

        if (!_factories.TryGetValue(serviceOptionType, out var factory))
        {
            ServiceLogger.NoFactoryRegistered(_logger, serviceOptionType);
            ServiceLogger.FactoryLookupMiss(
                _logger, GetType().Name, serviceOptionType, identifier,
                _factories.Count == 0 ? "<empty>" : string.Join(", ", _factories.Keys));
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("NoFactoryRegistered"),
                ResultDetails.Create("ServiceOptionType", serviceOptionType, "Identifier", identifier));
        }

        ServiceLogger.FactoryLookupSucceeded(_logger, serviceOptionType);

        // Why the async overload wins when the factory offers one: a domain whose creation resolves a
        // secret cannot do it in the sync Create, and reaching it any other way would need a provider
        // of its own.
        return factory is IAsyncServiceFactory<TService> asyncFactory
            ? await asyncFactory.Create(configuration, cancellationToken).ConfigureAwait(false)
            : Create(factory, configuration);
    }

    // ── Typed views ─────────────────────────────────────────────────────────
    // Why explicit: the constraint here is T : IGenericService, which the domain-typed Get above does
    // not carry. Explicit implementation lets both live on one class.

    async Task<IGenericResult<T>> IPlatformServiceProvider.Get<T>(string name, CancellationToken cancellationToken)
        => Cast<T>(await Get(name, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<T>> IPlatformServiceProvider.Get<T>(Guid id, CancellationToken cancellationToken)
        => Cast<T>(await Get(id, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<IReadOnlyList<T>>> IPlatformServiceProvider.Get<T>(CancellationToken cancellationToken)
    {
        var result = await Get(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess) return result.ToNewResult<IReadOnlyList<T>>();
        return GenericResult<IReadOnlyList<T>>.Success(result.Value?.OfType<T>().ToList() ?? []);
    }

    /// <summary>Narrows a resolved service to a more specific type.</summary>
    /// <typeparam name="T">The type to narrow to.</typeparam>
    /// <param name="result">The resolved service.</param>
    /// <returns>The narrowed service, or the original failure.</returns>
    protected static IGenericResult<T> Cast<T>(IGenericResult<TService> result)
        where T : IGenericService
    {
        if (result.IsSuccess)
        {
            // Why a failure and not a widened success: the caller asked for a T, and a service that is
            // not one cannot be returned as one. Carrying the success forward would hand back a null.
            return result.Value is T typed
                ? GenericResult<T>.Success(typed)
                : GenericResult<T>.Failure(
                    ServicesResultCodes.ByName("ServiceCastFailed"),
                    ResultDetails.Create("ExpectedType", typeof(T).Name,
                                         "ActualType", result.Value?.GetType().Name ?? "(null)"));
        }

        return result.ToNewResult<T>();
    }

    /// <inheritdoc />
    // Why empty rather than enumerating: a domain's members are configuration rows, and listing every
    // service means building every one. A caller that wants the set asks the configuration provider.
    public virtual Task<IGenericResult<IReadOnlyList<TService>>> Get(CancellationToken cancellationToken = default)
        => Task.FromResult(GenericResult<IReadOnlyList<TService>>.Success([]));

    /// <inheritdoc />
    public virtual Task<IGenericResult<TService>> Get(IGenericConfiguration configuration, CancellationToken cancellationToken = default)
        => configuration is TConfiguration typed
            ? Get(typed, cancellationToken)
            : Task.FromResult(GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("InvalidConfigurationType"),
                ResultDetails.Create("ExpectedType", typeof(TConfiguration).Name,
                                     "ActualType", configuration?.GetType().Name ?? "(null)")));

    /// <inheritdoc />
    // Why the caller's configuration is used as given: it was resolved once, in whatever context the
    // caller had. Re-resolving it here would run under a different one, and row-level security can
    // return a different row for the same name.
    public virtual Task<IGenericResult<TService>> Get(TConfiguration configuration, CancellationToken cancellationToken = default)
        => configuration is null
            ? Task.FromResult(GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("ConfigurationRequired"),
                ResultDetails.Create("ServiceType", typeof(TService).Name)))
            : CreateFrom(configuration, configuration.Name, cancellationToken);
}
