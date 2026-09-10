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
    private readonly Dictionary<string, Func<IServiceProvider, IServiceFactory<TService>>> _factories
        = new(StringComparer.OrdinalIgnoreCase);
    private readonly IServiceProvider? _services;
    private IDomainConfigurationProvider<TConfiguration>? _domainConfigurationProvider;

    /// <summary>Gets the registered factory resolvers keyed by implementation.</summary>
    protected IDictionary<string, Func<IServiceProvider, IServiceFactory<TService>>> Factories => _factories;

    /// <summary>Gets the domain's parent configuration provider.</summary>
    protected IDomainConfigurationProvider<TConfiguration>? DomainConfigurationProvider => _domainConfigurationProvider;

    private static readonly Dictionary<string, Func<IServiceProvider, IServiceFactory<TService>>> _registered
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers the factory for one service option type. Called from that option's Register method.
    /// </summary>
    /// <param name="implementation">The option's discriminator.</param>
    /// <param name="factory">Resolves the factory once the container exists.</param>
    public static void Register(string implementation, Func<IServiceProvider, IServiceFactory<TService>> factory)
    {
        if (string.IsNullOrEmpty(implementation))
            throw new ArgumentNullException(nameof(implementation));

        _registered[implementation] = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="PlatformServiceProviderBase{TService, TConfiguration, TFactory, TConfigurationProvider}"/> class.
    /// </summary>
    /// <param name="services">The scope's container, used to resolve the registered factories.</param>
    /// <param name="logger">The logger for this provider.</param>
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

        _services = services;

        // The resolver is copied, not invoked. Calling it here would resolve every factory once and
        // hand the same object back forever -- which quietly turns a transient registration into a
        // singleton. Invoking it at the point of use is what makes the registered lifetime mean what
        // it says.
        foreach (var registration in _registered)
        {
            _factories[registration.Key] = registration.Value;
            ServiceLogger.ProviderFactoryRegistered(_logger, registration.Key);
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

    }

    // ── Registration ────────────────────────────────────────────────────────

    /// <inheritdoc />
    public IGenericResult Register(string implementation, IServiceFactory<TService> factory)
    {
        _factories[implementation] = _ => factory;
        ServiceLogger.ProviderFactoryRegistered(_logger, implementation);
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
    /// <param name="factory">The factory registered for the configuration's Implementation.</param>
    /// <param name="configuration">The resolved (composed) configuration.</param>
    /// <returns>The created service, or a structured failure.</returns>
    private static IGenericResult<TService> Create(IServiceFactory<TService> factory, IGenericConfiguration configuration)
        => factory.Create(configuration);

    // ── Resolution ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    public virtual async Task<IGenericResult<TService>> Get(string name, CancellationToken cancellationToken = default)
        => await Build(name, ct => _domainConfigurationProvider!.Get(name, ct), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public virtual async Task<IGenericResult<TService>> Get(Guid id, CancellationToken cancellationToken = default)
        => await Build(id.ToString(), ct => _domainConfigurationProvider!.Get(id, ct), cancellationToken).ConfigureAwait(false);

    // One read. The configuration comes back carrying the domain it belongs to, so the factory is
    // chosen from the thing already in hand rather than by reading the domain row a second time.
    private async Task<IGenericResult<TService>> Build(
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

        if (!_factories.TryGetValue(configuration.Value.Domain, out var resolve) || _services is null)
        {
            ServiceLogger.NoFactoryRegistered(_logger, configuration.Value.Domain);
            ServiceLogger.FactoryLookupMiss(
                _logger, GetType().Name, configuration.Value.Domain, identifier,
                _factories.Count == 0 ? "<empty>" : string.Join(", ", _factories.Keys));
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("NoFactoryRegistered"),
                ResultDetails.Create("Implementation", configuration.Value.Domain, "Identifier", identifier));
        }

        // Invoked here, not cached: a factory registered transient hands back a new instance, a
        // singleton the same one -- whichever the application asked for.
        var factory = resolve(_services);
        ServiceLogger.FactoryLookupSucceeded(_logger, configuration.Value.Domain);

        return (factory is IAsyncServiceFactory<TService> asyncFactory
            ? await asyncFactory.Create(configuration.Value, cancellationToken).ConfigureAwait(false)
            : Create(factory, configuration.Value))
            ?? GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("InvalidFactoryType"),
                ResultDetails.Create("Implementation", configuration.Value.Domain,
                                     "FactoryType", factory.GetType().Name));
    }

    // ── Typed views ─────────────────────────────────────────────────────────

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

}
