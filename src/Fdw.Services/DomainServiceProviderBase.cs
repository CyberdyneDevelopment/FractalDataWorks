using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
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
public abstract class DomainServiceProviderBase<TService, TConfiguration, TFactory, TConfigurationProvider>
    : IDomainServiceProvider<TService, TConfiguration, TFactory, TConfigurationProvider>
    where TService : IGenericService
    where TConfiguration : class, IImplementationConfiguration
    where TFactory : IServiceFactory<TService>
    where TConfigurationProvider : IDomainConfigurationProvider<TConfiguration>
{
    private readonly ILogger<DomainServiceProviderBase<TService, TConfiguration, TFactory, TConfigurationProvider>> _logger;
    private readonly Dictionary<string, Func<IImplementationServiceProvider<TService, TConfiguration>>> _implementations
        = new(StringComparer.OrdinalIgnoreCase);
    private IDomainConfigurationProvider<TConfiguration>? _domainConfigurationProvider;

    /// <summary>Gets the registered implementation-provider factories keyed by implementation.</summary>
    protected IDictionary<string, Func<IImplementationServiceProvider<TService, TConfiguration>>> Implementations => _implementations;

    /// <summary>Gets the domain's parent configuration provider.</summary>
    protected IDomainConfigurationProvider<TConfiguration>? DomainConfigurationProvider => _domainConfigurationProvider;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="DomainServiceProviderBase{TService, TConfiguration, TFactory, TConfigurationProvider}"/> class.
    /// </summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <remarks>
    /// No container here, and nothing drained at construction. An implementation provider reaches
    /// this registry because a three-phase Register body handed it over, which is where every DI
    /// registration in this framework already happens; this type only keeps what it was given.
    /// </remarks>
    protected DomainServiceProviderBase(
        ILogger<DomainServiceProviderBase<TService, TConfiguration, TFactory, TConfigurationProvider>> logger)
        => _logger = logger;

    // ── Registration ────────────────────────────────────────────────────────

    /// <inheritdoc />
    public IGenericResult Register(
        string implementation,
        Func<IImplementationServiceProvider<TService, TConfiguration>> implementationProviderFactory)
    {
        _implementations[implementation] = implementationProviderFactory;
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

        if (!_implementations.TryGetValue(configuration.Value.Implementation, out var implementationProviderFactory))
        {
            ServiceLogger.NoFactoryRegistered(_logger, configuration.Value.Implementation);
            ServiceLogger.FactoryLookupMiss(
                _logger, GetType().Name, configuration.Value.Implementation, identifier,
                _implementations.Count == 0 ? "<empty>" : string.Join(", ", _implementations.Keys));
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("NoFactoryRegistered"),
                ResultDetails.Create("Implementation", configuration.Value.Implementation, "Identifier", identifier));
        }

        ServiceLogger.FactoryLookupSucceeded(_logger, configuration.Value.Implementation);

        // Invoked here, not stored: the implementation provider is resolved fresh on every call, so
        // whatever lifetime it is actually registered with — Scoped, Transient, Singleton — is honoured
        // every time, instead of freezing whatever the first resolution happened to return.
        var implementationProvider = implementationProviderFactory();

        // The implementation the row names resolves whatever it needs and builds. This provider does
        // not know what that is, and must not: knowing would mean knowing the implementation.
        return await implementationProvider
            .Create(configuration.Value, cancellationToken).ConfigureAwait(false);
    }

    // ── Typed views ─────────────────────────────────────────────────────────

    async Task<IGenericResult<T>> IDomainServiceProvider.Get<T>(string name, CancellationToken cancellationToken)
        => Cast<T>(await Get(name, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<T>> IDomainServiceProvider.Get<T>(Guid id, CancellationToken cancellationToken)
        => Cast<T>(await Get(id, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<IReadOnlyList<T>>> IDomainServiceProvider.Get<T>(CancellationToken cancellationToken)
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
