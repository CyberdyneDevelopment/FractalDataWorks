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
/// Generic base class for service providers.
/// One configuration provider for the domain, per-type child providers for typed config.
/// </summary>
public class DefaultServiceProvider<TService, TConfiguration, TFactory, TConfigurationProvider>
    : IFdwServiceProvider<TService, TConfiguration, TFactory, TConfigurationProvider>
    where TService : IGenericService
    where TConfiguration : class, IGenericConfiguration
    where TFactory : IServiceFactory<TService>
    where TConfigurationProvider : IServiceConfigurationProvider<TConfiguration>
{
    private readonly ILogger<DefaultServiceProvider<TService, TConfiguration, TFactory, TConfigurationProvider>> _logger;
    private readonly Dictionary<string, IServiceFactory<TService>> _factories = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the registered service factories keyed by service option type.</summary>
    protected IDictionary<string, IServiceFactory<TService>> Factories => _factories;
    private readonly Dictionary<string, IServiceConfigurationProvider<TConfiguration>> _configurationProviders = new(StringComparer.OrdinalIgnoreCase);
    private IServiceConfigurationProvider<TConfiguration>? _parentProvider;

    /// <summary>Gets the domain's configuration provider.</summary>
    protected IServiceConfigurationProvider<TConfiguration>? ParentProvider => _parentProvider;

    // ── The factory registry ────────────────────────────────────────────────────────────────────
    // An option's Register method registers whatever DI services its factory needs, then calls
    // Register(Name, func) here. The func is deferred because at that point the container is
    // not built yet; each scope's provider resolves it once, in its constructor.

    private static readonly Dictionary<string, Func<IServiceProvider, IServiceFactory<TService>>> _registered
        = new(StringComparer.OrdinalIgnoreCase);

    // Why a second static registry: an option that registers during Initialize has a LIVE container,
    // so it resolves its factory / typed configuration provider itself and hands over the instance via
    // the Register overloads below rather than a deferred func. That used to be enough because the
    // provider was a singleton — one instance held everything. The provider is now Scoped, so an
    // instance-only registration lives and dies with the startup scope and every later scope starts
    // empty ("No factory registered for service type 'MsSql'" on the first real request, long after
    // startup logged it registered). Promoting those registrations here keeps the ~23 Initialize-time
    // call sites working and restores the pre-Scoped sharing exactly: the instance was resolved once,
    // at startup, and is shared — the same lifetime the singleton provider gave it.
    private static readonly Dictionary<string, IServiceConfigurationProvider<TConfiguration>> _registeredConfigurationProviders
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers the factory for one service option type. Called from that option's Register method.
    /// </summary>
    /// <param name="serviceOptionType">The option's discriminator.</param>
    /// <param name="factory">Resolves the factory once the container exists.</param>
    // Why static: Register runs while the container is still being built, so there is no provider
    // instance yet — the provider is scoped and created later, once per scope.
    public static void Register(string serviceOptionType, Func<IServiceProvider, IServiceFactory<TService>> factory)
    {
        if (string.IsNullOrEmpty(serviceOptionType))
            throw new ArgumentNullException(nameof(serviceOptionType));

        _registered[serviceOptionType] = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultServiceProvider{TService, TConfiguration, TFactory, TConfigurationProvider}"/> class.
    /// </summary>
    /// <param name="services">The scope's container, used to resolve the registered factories.</param>
    /// <param name="logger">The logger for this provider.</param>
    // Why sp is resolved here and NOT stored: every registered func is invoked once, now, against this
    // scope. The provider keeps the resulting factories, never the container — so nothing can reach
    // back into DI at request time.
    public DefaultServiceProvider(
        IServiceProvider services,
        ILogger<DefaultServiceProvider<TService, TConfiguration, TFactory, TConfigurationProvider>> logger)
    {
        _logger = logger;

        if (services is null)
        {
            ServiceLogger.ContainerNotSupplied(_logger, GetType().Name);
            return;
        }

        // Why the provider names itself in every line: this type is generic and shared by ~12
        // domains, so "drained 0 registrations" is meaningless without saying WHICH provider drained
        // nothing. GetType() is the closed, derived type (DefaultConnectionProvider), not this base.
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

        // Why Critical and why here: this is the only moment the emptiness is knowable before some
        // later request fails on it. A provider with no factories cannot create anything for the
        // rest of its scope, so it is reported once, loudly, at the point of construction rather
        // than as a stream of per-request errors that name the symptom instead of the cause.
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
    // Why: the seam that lets a provider hand a factory something the factory must not resolve for
    // itself (which is what made a factory ctor-depend on its own provider and recurse during
    // provider realization — FDW-615). DefaultExternalIdentityProvisionerProvider overrides this to
    // pass `this` for Provision-time sibling lookup. The default is plain pure construction.
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

        // Why: a multi-level domain (header → kind → engine, e.g. Pipeline) registers its factories
        // under the ENGINE discriminator on the nested typed body, not the header's KIND. Drill one
        // level via IServiceDispatchHost; single-level domains (no marker) dispatch on their own type.
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
        return await CreateFromType(name, cfgType, parentResult.Value.Id, cancellationToken).ConfigureAwait(false);
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

        // Why: a multi-level domain (header → kind → engine, e.g. Pipeline) registers its factories
        // under the ENGINE discriminator on the nested typed body, not the header's KIND. Drill one
        // level via IServiceDispatchHost; single-level domains (no marker) dispatch on their own type.
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
        return await CreateFromType(configNameResolved, cfgType, parentResult.Value.Id, cancellationToken).ConfigureAwait(false);
    }
#pragma warning restore MA0051

    // ── Get (all) ───────────────────────────────────────────────────────────

    /// <inheritdoc />
    public virtual Task<IGenericResult<IReadOnlyList<TService>>> Get(CancellationToken cancellationToken = default)
        => Task.FromResult(GenericResult<IReadOnlyList<TService>>.Success([]));

    // ── Get (from configuration) ─────────────────────────────────────────────

    /// <inheritdoc />
    // Why: type-check + delegate to the typed overload. A configuration of the wrong concrete
    // type is a structured failure — never silently re-resolved by name.
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
    // Why: no name/id lookup — the caller already holds the configuration (e.g. resolved once at
    // Initialize in system context). The configuration's ServiceOptionType selects the factory via
    // the registered set (TypeCollection-backed); there is no switch over type names.
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

    // ── Generic casts (IFdwServiceProvider base) ────────────────────────────

    async Task<IGenericResult<T>> IFdwServiceProvider.Get<T>(string name, CancellationToken cancellationToken)
        => Cast<T>(await Get(name, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<T>> IFdwServiceProvider.Get<T>(Guid id, CancellationToken cancellationToken)
        => Cast<T>(await Get(id, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<IReadOnlyList<T>>> IFdwServiceProvider.Get<T>(CancellationToken cancellationToken)
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
    // Why: ONE cast mechanism for the whole provider family. The base needs it for its own
    // IFdwServiceProvider.Get{T} implementations and derived providers need it for their domain
    // interfaces (e.g. IDataConnectionProvider.Get{T}) — DefaultConnectionProvider used to carry a
    // byte-for-byte copy of this logic.
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

    // ── CreateFromType — child provider for typed config, factory for service ─

    // Why: Parent provider told us the ServiceOptionType. Now get the typed config
    // from the per-type child provider, then create via factory.
    // Why two paths: a domain whose child config type IS its own configuration type (Scheduling,
    // ExternalIdentityProviders) registers that provider here and is served by the typed branch below.
    // A composed-header domain (Connection, SecretManager, DataStore, and now Notifications) attaches
    // its typed body to the HEADER provider by discriminator instead, so this dictionary stays empty
    // and the miss falls through to CreateFromParentConfig — the expected path for those domains.
    private async Task<IGenericResult<TService>> CreateFromType(string name, string serviceOptionType, Guid parentId, CancellationToken cancellationToken)
    {
        if (!_configurationProviders.TryGetValue(serviceOptionType, out var configProvider))
        {
            return await CreateFromParentConfig(name, serviceOptionType, parentId, cancellationToken).ConfigureAwait(false);
        }

        // Why: parentId is the resolved parent record's Id — always use it when available.
        // Falling through to name lookup when an Id is present is a tenant-isolation hole:
        // a miss on the Id must be a hard failure, not an opportunity to find a record from
        // a different parent by name.
        ServiceLogger.CreatingFromTypedConfiguration(_logger, name, serviceOptionType, parentId);

        TConfiguration? config;
        if (parentId != Guid.Empty)
        {
            var byIdResult = await configProvider.Get(parentId, cancellationToken).ConfigureAwait(false);
            if (!byIdResult.IsSuccess || byIdResult.Value is null)
            {
                ServiceLogger.ConfigurationNotFound(_logger, name);
                return GenericResult<TService>.Failure(
                    ServicesResultCodes.ByName("ConfigurationNotFound"),
                    ResultDetails.Create("Identifier", name));
            }
            config = byIdResult.Value;
        }
        else
        {
            var byNameResult = await configProvider.Get(name, cancellationToken).ConfigureAwait(false);
            if (!byNameResult.IsSuccess || byNameResult.Value is null)
            {
                ServiceLogger.ConfigurationNotFound(_logger, name);
                return GenericResult<TService>.Failure(
                    ServicesResultCodes.ByName("ConfigurationNotFound"),
                    ResultDetails.Create("Identifier", name));
            }
            config = byNameResult.Value;
        }

        if (!_factories.TryGetValue(serviceOptionType, out var factory))
        {
            ServiceLogger.NoFactoryRegistered(_logger, serviceOptionType);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("NoFactoryRegistered"),
                ResultDetails.Create("ServiceOptionType", serviceOptionType));
        }

        ServiceLogger.FactoryLookupSucceeded(_logger, serviceOptionType);

        var result = Create(factory, config);
        if (result.IsSuccess)
            ServiceLogger.ServiceCreated(_logger, name, serviceOptionType);
        else
            ServiceLogger.ServiceCreationFailed(_logger, name, result.CurrentMessage ?? "Unknown error");

        return result;
    }

    // Why: Fallback path for composed-header domains (Connection, SecretManager, DataStore) where
    // _configurationProviders is empty because typed body providers are registered with the
    // header provider via discriminator dispatch. The parent provider (e.g.
    // SecretManagerConfigurationProvider) already ran PopulateTypedBody and set
    // header.Configuration before returning. Re-query parent to get the composed header,
    // then pass it to factory.Create. The factory extracts the typed body from the header.
    private async Task<IGenericResult<TService>> CreateFromParentConfig(
        string name, string serviceOptionType, Guid parentId, CancellationToken cancellationToken)
    {
        var parentProvider = _parentProvider;
        if (parentProvider is null)
        {
            ServiceLogger.NoFactoryRegistered(_logger, serviceOptionType);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("NoFactoryRegistered"),
                ResultDetails.Create("ServiceOptionType", serviceOptionType));
        }

        if (!_factories.TryGetValue(serviceOptionType, out var factory))
        {
            ServiceLogger.NoFactoryRegistered(_logger, serviceOptionType);
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("NoFactoryRegistered"),
                ResultDetails.Create("ServiceOptionType", serviceOptionType));
        }

        // Why: Re-query by Id if we have one; otherwise fall through to name.
        // The parent provider runs PopulateTypedBody on its Get() result, so the returned
        // config already has .Configuration (the typed body) attached. byId failure (not
        // found / lookup error) is non-fatal — we simply fall back to the name-based path.
        TConfiguration? config = null;
        if (parentId != Guid.Empty)
        {
            var byIdResult = await parentProvider.Get(parentId, cancellationToken).ConfigureAwait(false);
            if (byIdResult.IsSuccess && byIdResult.Value is not null)
                config = byIdResult.Value;
            else
            {
                // Why: Id-based lookup miss falls through to the name path below; this is
                // a deliberate dual-resolve, not an error. Log only.
                ServiceLogger.ConfigurationNotFound(_logger, parentId.ToString());
            }
        }

        if (config is null)
        {
            var byNameResult = await parentProvider.Get(name, cancellationToken).ConfigureAwait(false);
            if (!byNameResult.IsSuccess || byNameResult.Value is null)
            {
                ServiceLogger.ConfigurationNotFound(_logger, name);
                return GenericResult<TService>.Failure(
                    ServicesResultCodes.ByName("ConfigurationNotFound"),
                    ResultDetails.Create("Identifier", name));
            }
            config = byNameResult.Value;
        }

        ServiceLogger.FactoryLookupSucceeded(_logger, serviceOptionType);

        var result = Create(factory, config);
        if (result.IsSuccess)
            ServiceLogger.ServiceCreated(_logger, name, serviceOptionType);
        else
            ServiceLogger.ServiceCreationFailed(_logger, name, result.CurrentMessage ?? "Unknown error");

        return result;
    }
}
