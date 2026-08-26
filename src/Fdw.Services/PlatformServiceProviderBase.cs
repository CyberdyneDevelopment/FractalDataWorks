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
    where TConfiguration : class, IGenericConfiguration
    where TFactory : IServiceFactory<TService>
    where TConfigurationProvider : IServiceConfigurationProvider<TConfiguration>
{
    private readonly ILogger<PlatformServiceProviderBase<TService, TConfiguration, TFactory, TConfigurationProvider>> _logger;
    private readonly Dictionary<string, IServiceFactory<TService>> _factories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IServiceConfigurationProvider<TConfiguration>> _configurationProviders = new(StringComparer.OrdinalIgnoreCase);
    private IServiceConfigurationProvider<TConfiguration>? _parentProvider;

    /// <summary>Gets the registered service factories keyed by service option type.</summary>
    protected IDictionary<string, IServiceFactory<TService>> Factories => _factories;

    /// <summary>Gets the domain's configuration provider.</summary>
    protected IServiceConfigurationProvider<TConfiguration>? ParentProvider => _parentProvider;

    private static readonly Dictionary<string, Func<IServiceProvider, IServiceFactory<TService>>> _registered
        = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, IServiceConfigurationProvider<TConfiguration>> _registeredConfigurationProviders
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

    /// <inheritdoc />
    public IGenericResult Register(string serviceOptionType, IServiceFactory<TService> factory)
    {
        _factories[serviceOptionType] = factory;
        _registered[serviceOptionType] = _ => factory;
        ServiceLogger.ProviderFactoryRegistered(_logger, serviceOptionType);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public IGenericResult Register(string serviceOptionType, IServiceConfigurationProvider<TConfiguration> configurationProvider)
    {
        _configurationProviders[serviceOptionType] = configurationProvider;
        _registeredConfigurationProviders[serviceOptionType] = configurationProvider;
        ServiceLogger.ProviderConfigurationRegistered(_logger, serviceOptionType);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public IGenericResult Register(IServiceConfigurationProvider<TConfiguration> parentProvider)
    {
        _parentProvider = parentProvider;
        ServiceLogger.ParentProviderRegistered(_logger);
        return GenericResult.Success();
    }

    /// <summary>
    /// Invokes the registered factory to build the service. Override to supply additional
    /// already-resolved dependencies to a domain-specific <c>Create</c> overload.
    /// </summary>
    /// <param name="factory">The factory registered for the configuration's ServiceOptionType.</param>
    /// <param name="configuration">The resolved (composed) configuration.</param>
    /// <returns>The created service, or a structured failure.</returns>
    protected virtual IGenericResult<TService> Create(IServiceFactory<TService> factory, TConfiguration configuration)
        => factory.Create(configuration);

    // ── Get by name ─────────────────────────────────────────────────────────

    /// <inheritdoc />
#pragma warning disable MA0051 // Why: sequential async resolution steps — not cyclomatic complexity
    public virtual async Task<IGenericResult<TService>> Get(string name, CancellationToken cancellationToken = default)
    {
        ServiceLogger.GettingServiceByName(_logger, name);

        if (_parentProvider is null)
        {
            ServiceLogger.NoParentProviderRegistered(_logger, name);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("NoParentProvider"),
                ResultDetails.Create("Identifier", name));
        }

        var parentResult = await _parentProvider.Get(name, cancellationToken).ConfigureAwait(false);
        if (!parentResult.IsSuccess || parentResult.Value is null)
        {
            ServiceLogger.ConfigurationNotFound(_logger, name);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("ConfigurationNotFound"),
                ResultDetails.Create("Identifier", name));
        }

        // Why the drill: a multi-level domain (header → kind → engine, e.g. Pipeline) registers its
        // factories under the ENGINE discriminator on the nested typed body, not the header's KIND.
        var cfgType = (parentResult.Value as IServiceDispatchHost)?.ServiceDispatchBody?.ServiceOptionType
            ?? parentResult.Value.ServiceOptionType;
        if (string.IsNullOrEmpty(cfgType))
        {
            ServiceLogger.ServiceOptionTypeMissing(_logger, name);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("ServiceOptionTypeMissing"),
                ResultDetails.Create("Identifier", name));
        }

        ServiceLogger.ResolvedViaParentConfig(_logger, name, cfgType);
        return await ResolveAndCreate(name, cfgType, parentResult.Value.Id, cancellationToken).ConfigureAwait(false);
    }
#pragma warning restore MA0051

    // ── Get by id ───────────────────────────────────────────────────────────

    /// <inheritdoc />
#pragma warning disable MA0051 // Why: sequential async resolution steps — not cyclomatic complexity
    public virtual async Task<IGenericResult<TService>> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var idString = id.ToString();
        ServiceLogger.GettingServiceById(_logger, idString);

        if (_parentProvider is null)
        {
            ServiceLogger.NoParentProviderRegistered(_logger, idString);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("NoParentProvider"),
                ResultDetails.Create("Identifier", idString));
        }

        var parentResult = await _parentProvider.Get(id, cancellationToken).ConfigureAwait(false);
        if (!parentResult.IsSuccess || parentResult.Value is null)
        {
            ServiceLogger.ConfigurationNotFound(_logger, idString);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("ConfigurationNotFound"),
                ResultDetails.Create("Identifier", idString));
        }

        var cfgType = (parentResult.Value as IServiceDispatchHost)?.ServiceDispatchBody?.ServiceOptionType
            ?? parentResult.Value.ServiceOptionType;
        if (string.IsNullOrEmpty(cfgType))
        {
            var configName = parentResult.Value.Name ?? idString;
            ServiceLogger.ServiceOptionTypeMissing(_logger, configName);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("ServiceOptionTypeMissing"),
                ResultDetails.Create("Identifier", configName));
        }

        var configNameResolved = parentResult.Value.Name ?? idString;
        ServiceLogger.ResolvedViaParentConfig(_logger, configNameResolved, cfgType);
        return await ResolveAndCreate(configNameResolved, cfgType, parentResult.Value.Id, cancellationToken).ConfigureAwait(false);
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

    // ── Evict ───────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public virtual void Evict(string name) { }

    /// <inheritdoc />
    public virtual void Evict(Guid id) { }

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

    // ── Resolve the configuration, then create ───────────────────────────────

    // Why one path where there were two: every call site that registers a per-discriminator
    // configuration provider registers the very same object it registers as the parent, so the
    // typed branch and the parent branch always queried the same instance — while disagreeing
    // about whether an Id miss was fatal. A domain's tenant-isolation strictness therefore
    // depended on which registration it happened to make. The lookup order survives, so a
    // genuinely distinct typed provider still wins; the failure semantics no longer vary.
    private async Task<IGenericResult<TService>> ResolveAndCreate(
        string name, string serviceOptionType, Guid parentId, CancellationToken cancellationToken)
    {
        if (!_factories.TryGetValue(serviceOptionType, out var factory))
        {
            ServiceLogger.NoFactoryRegistered(_logger, serviceOptionType);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("NoFactoryRegistered"),
                ResultDetails.Create("ServiceOptionType", serviceOptionType));
        }

        var configProvider = _configurationProviders.TryGetValue(serviceOptionType, out var typedProvider)
            ? typedProvider
            : _parentProvider;

        if (configProvider is null)
        {
            ServiceLogger.NoParentProviderRegistered(_logger, name);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("NoParentProvider"),
                ResultDetails.Create("Identifier", name));
        }

        ServiceLogger.CreatingFromTypedConfiguration(_logger, name, serviceOptionType, parentId);

        // Why the Id wins outright when present: it is the parent record that was just resolved, so
        // a miss is an inconsistency — never an invitation to find a record from a DIFFERENT parent
        // by name.
        var configResult = parentId != Guid.Empty
            ? await configProvider.Get(parentId, cancellationToken).ConfigureAwait(false)
            : await configProvider.Get(name, cancellationToken).ConfigureAwait(false);

        if (!configResult.IsSuccess || configResult.Value is null)
        {
            ServiceLogger.ConfigurationNotFound(_logger, name);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("ConfigurationNotFound"),
                ResultDetails.Create("Identifier", name));
        }

        ServiceLogger.FactoryLookupSucceeded(_logger, serviceOptionType);

        var result = Create(factory, configResult.Value);
        if (result.IsSuccess)
            ServiceLogger.ServiceCreated(_logger, name, serviceOptionType);
        else
            ServiceLogger.ServiceCreationFailed(_logger, name, result.CurrentMessage ?? "Unknown error");

        return result;
    }
}
