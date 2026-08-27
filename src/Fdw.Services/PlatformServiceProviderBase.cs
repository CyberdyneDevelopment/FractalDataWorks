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
    public IGenericResult Register<TConcrete>(string serviceOptionType, IServiceConfigurationProvider<TConcrete> configurationProvider)
        where TConcrete : class, TConfiguration
    {
        var erased = Erase(configurationProvider);
        _configurationProviders[serviceOptionType] = erased;
        _registeredConfigurationProviders[serviceOptionType] = erased;
        ServiceLogger.ProviderConfigurationRegistered(_logger, serviceOptionType);
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
    private IGenericResult<TService> Create(IServiceFactory<TService> factory, TConfiguration configuration)
        => factory.Create(configuration);

    // ── Get by name ─────────────────────────────────────────────────────────

    /// <inheritdoc />
#pragma warning disable MA0051 // Why: sequential async resolution steps — not cyclomatic complexity
    public virtual async Task<IGenericResult<TService>> Get(string name, CancellationToken cancellationToken = default)
    {
        ServiceLogger.GettingServiceByName(_logger, name);

        if (_domainConfigurationProvider is null)
        {
            ServiceLogger.NoDomainConfigurationProviderRegistered(_logger, name);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("NoDomainConfigurationProvider"),
                ResultDetails.Create("Identifier", name));
        }

        var domainResult = await _domainConfigurationProvider.Get(name, cancellationToken).ConfigureAwait(false);
        if (!domainResult.IsSuccess || domainResult.Value is null)
        {
            ServiceLogger.ConfigurationNotFound(_logger, name);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("ConfigurationNotFound"),
                ResultDetails.Create("Identifier", name));
        }

        // Why the drill: a multi-level domain (header → kind → engine, e.g. Pipeline) registers its
        // factories under the ENGINE discriminator on the nested typed body, not the header's KIND.
        var cfgType = (domainResult.Value as IServiceDispatchHost)?.ServiceDispatchBody?.ServiceOptionType
            ?? domainResult.Value.ServiceOptionType;
        if (string.IsNullOrEmpty(cfgType))
        {
            ServiceLogger.ServiceOptionTypeMissing(_logger, name);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("ServiceOptionTypeMissing"),
                ResultDetails.Create("Identifier", name));
        }

        ServiceLogger.ResolvedViaParentConfig(_logger, name, cfgType);
        return await ResolveAndCreate(name, cfgType, domainResult.Value.Id, cancellationToken).ConfigureAwait(false);
    }
#pragma warning restore MA0051

    // ── Get by id ───────────────────────────────────────────────────────────

    /// <inheritdoc />
#pragma warning disable MA0051 // Why: sequential async resolution steps — not cyclomatic complexity
    public virtual async Task<IGenericResult<TService>> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var idString = id.ToString();
        ServiceLogger.GettingServiceById(_logger, idString);

        if (_domainConfigurationProvider is null)
        {
            ServiceLogger.NoDomainConfigurationProviderRegistered(_logger, idString);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("NoDomainConfigurationProvider"),
                ResultDetails.Create("Identifier", idString));
        }

        var domainResult = await _domainConfigurationProvider.Get(id, cancellationToken).ConfigureAwait(false);
        if (!domainResult.IsSuccess || domainResult.Value is null)
        {
            ServiceLogger.ConfigurationNotFound(_logger, idString);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("ConfigurationNotFound"),
                ResultDetails.Create("Identifier", idString));
        }

        var cfgType = (domainResult.Value as IServiceDispatchHost)?.ServiceDispatchBody?.ServiceOptionType
            ?? domainResult.Value.ServiceOptionType;
        if (string.IsNullOrEmpty(cfgType))
        {
            var configName = domainResult.Value.Name ?? idString;
            ServiceLogger.ServiceOptionTypeMissing(_logger, configName);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("ServiceOptionTypeMissing"),
                ResultDetails.Create("Identifier", configName));
        }

        var configNameResolved = domainResult.Value.Name ?? idString;
        ServiceLogger.ResolvedViaParentConfig(_logger, configNameResolved, cfgType);
        return await ResolveAndCreate(configNameResolved, cfgType, domainResult.Value.Id, cancellationToken).ConfigureAwait(false);
    }
#pragma warning restore MA0051

    // ── Get (all) ───────────────────────────────────────────────────────────

    /// <inheritdoc />
    public virtual Task<IGenericResult<IReadOnlyList<TService>>> Get(CancellationToken cancellationToken = default)
        => Task.FromResult(GenericResult<IReadOnlyList<TService>>.Success([]));

    // ── Get (from configuration) ─────────────────────────────────────────────

    /// <inheritdoc />
    public virtual Task<IGenericResult<TService>> Get(IGenericConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (configuration is null)
        {
            ServiceLogger.ConfigurationNotFound(_logger, "(null)");
            return Task.FromResult(GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("ConfigurationNotFound"),
                ResultDetails.Create("Identifier", "(null)")));
        }

        if (configuration is not TConfiguration typed)
        {
            ServiceLogger.CastFailed(_logger, typeof(TConfiguration).Name, configuration.GetType().Name);
            return Task.FromResult(GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("InvalidConfigurationType"),
                ResultDetails.Create("ExpectedType", typeof(TConfiguration).Name, "ActualType", configuration.GetType().Name)));
        }

        return Get(typed, cancellationToken);
    }

    /// <inheritdoc />
    // Why no switch over type names: the configuration's ServiceOptionType selects the factory via
    // the registered set, which is TypeCollection-backed.
    public virtual Task<IGenericResult<TService>> Get(TConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (configuration is null)
        {
            ServiceLogger.ConfigurationNotFound(_logger, "(null)");
            return Task.FromResult(GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("ConfigurationNotFound"),
                ResultDetails.Create("Identifier", "(null)")));
        }

        var serviceOptionType = configuration.ServiceOptionType;
        if (string.IsNullOrWhiteSpace(serviceOptionType))
        {
            ServiceLogger.ServiceOptionTypeMissing(_logger, configuration.Name);
            return Task.FromResult(GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("ServiceOptionTypeMissing"),
                ResultDetails.Create("Identifier", configuration.Name)));
        }

        if (!_factories.TryGetValue(serviceOptionType, out var factory))
        {
            ServiceLogger.NoFactoryRegistered(_logger, serviceOptionType);
            return Task.FromResult(GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("NoFactoryRegistered"),
                ResultDetails.Create("ServiceOptionType", serviceOptionType)));
        }

        ServiceLogger.FactoryLookupSucceeded(_logger, serviceOptionType);

        var result = Create(factory, configuration);
        if (result.IsSuccess)
            ServiceLogger.ServiceCreated(_logger, configuration.Name, serviceOptionType);
        else
            ServiceLogger.ServiceCreationFailed(_logger, configuration.Name, result.CurrentMessage ?? "Unknown error");

        return Task.FromResult(result);
    }
    // ── Generic casts (IPlatformServiceProvider base) ────────────────────────────

    async Task<IGenericResult<T>> IPlatformServiceProvider.Get<T>(string name, CancellationToken cancellationToken)
        => Cast<T>(await Get(name, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<T>> IPlatformServiceProvider.Get<T>(Guid id, CancellationToken cancellationToken)
        => Cast<T>(await Get(id, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<IReadOnlyList<T>>> IPlatformServiceProvider.Get<T>(CancellationToken cancellationToken)
    {
        var result = await Get(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess) return result.ToNewResult<IReadOnlyList<T>>();
        var typed = result.Value?.OfType<T>().ToList() ?? [];
        return GenericResult<IReadOnlyList<T>>.Success(typed);
    }

    /// <summary>
    /// Narrows a service result to a more specific service interface, failing loud on a type mismatch.
    /// </summary>
    /// <typeparam name="T">The interface the caller asked for.</typeparam>
    /// <param name="result">The result produced by one of the Get overloads.</param>
    /// <returns>The narrowed result, or a structured cast failure.</returns>
    // Why protected: derived providers need it for their own domain interfaces (e.g.
    // IDataConnectionProvider.Get{T}), which is where a byte-for-byte copy of this used to live.
    protected IGenericResult<T> Cast<T>(IGenericResult<TService> result)
    {
        if (!result.IsSuccess) return result.ToNewResult<T>();
        if (result.Value is T typed) return result.ToNewResult(typed);

        var expectedType = typeof(T).Name;
        var actualType = result.Value?.GetType().Name ?? "null";
        ServiceLogger.CastFailed(_logger, expectedType, actualType);
        return GenericResult<T>.Failure(
            ServicesResultCodes.ByName("ServiceCastFailed"),
            ResultDetails.Create("ExpectedType", expectedType, "ActualType", actualType));
    }

    /// <summary>
    /// Resolves the implementation configuration behind a domain configuration name.
    /// </summary>
    /// <param name="name">The domain configuration's name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The implementation configuration, or a structured failure.</returns>
    /// <remarks>
    /// For domains that need the configuration without building the service — one that caches by name,
    /// say. It takes the same two steps the create path takes: the domain configuration supplies the Id
    /// and the ServiceOptionType, and the implementation configuration provider registered for that type
    /// supplies the configuration itself.
    /// </remarks>
    protected async Task<IGenericResult<TConfiguration>> ResolveConfiguration(
        string name, CancellationToken cancellationToken = default)
    {
        if (_domainConfigurationProvider is null)
        {
            ServiceLogger.NoDomainConfigurationProviderRegistered(_logger, name);
            return GenericResult<TConfiguration>.Failure(
                ServicesResultCodes.ByName("NoDomainConfigurationProvider"),
                ResultDetails.Create("Identifier", name));
        }

        var domainResult = await _domainConfigurationProvider.Get(name, cancellationToken).ConfigureAwait(false);
        return await ResolveFromDomainRecord(name, domainResult, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves the implementation configuration behind a domain configuration id.
    /// </summary>
    /// <param name="id">The domain configuration's durable id.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The implementation configuration, or a structured failure.</returns>
    protected async Task<IGenericResult<TConfiguration>> ResolveConfiguration(
        Guid id, CancellationToken cancellationToken = default)
    {
        var identifier = id.ToString();
        if (_domainConfigurationProvider is null)
        {
            ServiceLogger.NoDomainConfigurationProviderRegistered(_logger, identifier);
            return GenericResult<TConfiguration>.Failure(
                ServicesResultCodes.ByName("NoDomainConfigurationProvider"),
                ResultDetails.Create("Identifier", identifier));
        }

        var domainResult = await _domainConfigurationProvider.Get(id, cancellationToken).ConfigureAwait(false);
        return await ResolveFromDomainRecord(identifier, domainResult, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult<TConfiguration>> ResolveFromDomainRecord(
        string identifier,
        IGenericResult<IGenericConfiguration> domainResult,
        CancellationToken cancellationToken)
    {
        if (!domainResult.IsSuccess || domainResult.Value is null)
        {
            ServiceLogger.ConfigurationNotFound(_logger, identifier);
            return GenericResult<TConfiguration>.Failure(
                ServicesResultCodes.ByName("ConfigurationNotFound"),
                ResultDetails.Create("Identifier", identifier));
        }

        var serviceOptionType = (domainResult.Value as IServiceDispatchHost)?.ServiceDispatchBody?.ServiceOptionType
            ?? domainResult.Value.ServiceOptionType;
        if (string.IsNullOrEmpty(serviceOptionType))
        {
            ServiceLogger.ServiceOptionTypeMissing(_logger, identifier);
            return GenericResult<TConfiguration>.Failure(
                ServicesResultCodes.ByName("ServiceOptionTypeMissing"),
                ResultDetails.Create("Identifier", identifier));
        }

        if (!_configurationProviders.TryGetValue(serviceOptionType, out var configProvider))
        {
            ServiceLogger.NoConfigurationProviderRegistered(_logger, identifier, serviceOptionType);
            return GenericResult<TConfiguration>.Failure(
                ServicesResultCodes.ByName("NoConfigurationProvider"),
                ResultDetails.Create("ServiceOptionType", serviceOptionType, "Identifier", identifier));
        }

        var configResult = domainResult.Value.Id != Guid.Empty
            ? await configProvider.Get(domainResult.Value.Id, cancellationToken).ConfigureAwait(false)
            : await configProvider.Get(identifier, cancellationToken).ConfigureAwait(false);

        if (!configResult.IsSuccess || configResult.Value is null)
        {
            ServiceLogger.ConfigurationNotFound(_logger, identifier);
            return GenericResult<TConfiguration>.Failure(
                ServicesResultCodes.ByName("ConfigurationNotFound"),
                ResultDetails.Create("Identifier", identifier));
        }

        if (configResult.Value is not TConfiguration typed)
        {
            ServiceLogger.CastFailed(_logger, typeof(TConfiguration).Name, configResult.Value.GetType().Name);
            return GenericResult<TConfiguration>.Failure(
                ServicesResultCodes.ByName("InvalidConfigurationType"),
                ResultDetails.Create("ExpectedType", typeof(TConfiguration).Name,
                                     "ActualType", configResult.Value.GetType().Name));
        }

        return GenericResult<TConfiguration>.Success(typed);
    }

    // ── Resolve the configuration, then create ───────────────────────────────

    // Why one path where there were two: every call site that registers a per-discriminator
    // configuration provider registers the very same object it registers as the parent, so the
    // typed branch and the parent branch always queried the same instance — while disagreeing
    // about whether an Id miss was fatal. A domain's tenant-isolation strictness therefore
    // depended on which registration it happened to make. The lookup order survives, so a
    // genuinely distinct typed provider still wins; the failure semantics no longer vary.
    private async Task<IGenericResult<TService>> ResolveAndCreate(
        string name, string serviceOptionType, Guid domainConfigurationId, CancellationToken cancellationToken)
    {
        if (!_factories.TryGetValue(serviceOptionType, out var factory))
        {
            ServiceLogger.NoFactoryRegistered(_logger, serviceOptionType);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("NoFactoryRegistered"),
                ResultDetails.Create("ServiceOptionType", serviceOptionType));
        }

        // Why not fall back to the parent provider here: the parent yields the child's Id and
        // ServiceOptionType and nothing else. Reading the configuration off it instead would resolve a
        // record of the wrong shape for every domain whose option carries a typed body, and would do it
        // silently — the miss is a missing registration, so it fails as one.
        if (!_configurationProviders.TryGetValue(serviceOptionType, out var configProvider))
        {
            ServiceLogger.NoConfigurationProviderRegistered(_logger, name, serviceOptionType);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("NoConfigurationProvider"),
                ResultDetails.Create("ServiceOptionType", serviceOptionType,
                                     "Identifier", name));
        }

        ServiceLogger.CreatingFromTypedConfiguration(_logger, name, serviceOptionType, domainConfigurationId);

        // Why the Id wins outright when present: it is the domain record that was just resolved, so a
        // miss is an inconsistency — never an invitation to find a record from a DIFFERENT domain
        // configuration by name.
        var configResult = domainConfigurationId != Guid.Empty
            ? await configProvider.Get(domainConfigurationId, cancellationToken).ConfigureAwait(false)
            : await configProvider.Get(name, cancellationToken).ConfigureAwait(false);

        if (!configResult.IsSuccess || configResult.Value is null)
        {
            ServiceLogger.ConfigurationNotFound(_logger, name);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("ConfigurationNotFound"),
                ResultDetails.Create("Identifier", name));
        }

        // Why the cast is here and checked: the configuration comes back through the erased provider
        // view as IGenericConfiguration, but the factory is typed. A configuration of the wrong
        // concrete type is a real defect — a domain wired to another domain's provider — so it fails
        // with the type it got, rather than being coerced or skipped.
        if (configResult.Value is not TConfiguration typedConfig)
        {
            ServiceLogger.CastFailed(_logger, typeof(TConfiguration).Name, configResult.Value.GetType().Name);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("InvalidConfigurationType"),
                ResultDetails.Create("ExpectedType", typeof(TConfiguration).Name,
                                     "ActualType", configResult.Value.GetType().Name));
        }

        ServiceLogger.FactoryLookupSucceeded(_logger, serviceOptionType);

        var result = Create(factory, typedConfig);
        if (result.IsSuccess)
            ServiceLogger.ServiceCreated(_logger, name, serviceOptionType);
        else
            ServiceLogger.ServiceCreationFailed(_logger, name, result.CurrentMessage ?? "Unknown error");

        return result;
    }
}
