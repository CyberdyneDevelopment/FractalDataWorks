using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Logging;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.Connections.Logging;
using Fdw.ServiceTypes;

namespace Fdw.Services.Connections;

/// <summary>
/// Default implementation of IConnectionProvider and IDataConnectionProvider.
/// </summary>
/// <remarks>
/// The provider is a name -> connection lookup with ONE creation path: the header provider composes
/// the configuration, the option named by the header's discriminator supplies the factory type, and
/// that factory builds the connection. Every public overload funnels through it. Nothing is cached.
/// </remarks>
public sealed class DefaultConnectionProvider
    : PlatformServiceProviderBase<IGenericConnection, IConnectionImplementationConfiguration, IServiceFactory<IGenericConnection>, IServiceConfigurationProvider<IConnectionImplementationConfiguration>>,
      IConnectionProvider,
      IDataConnectionProvider
{
    private readonly ILogger<DefaultConnectionProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultConnectionProvider"/> class.
    /// </summary>
    /// <param name="services">The container this provider resolves factories from.</param>
    /// <param name="logger">The logger instance.</param>
    public DefaultConnectionProvider(
        IServiceProvider services,
        ILogger<DefaultConnectionProvider> logger)
        : base(services, logger ?? NullLogger<DefaultConnectionProvider>.Instance)
    {
        _logger = logger ?? NullLogger<DefaultConnectionProvider>.Instance;
    }

    /// <summary>
    /// Gets a connection built from an already-resolved composed-header configuration.
    /// </summary>
    // Why: the caller (e.g. a DataVault) resolved the configuration once in system context;
    // re-resolving by name at request time would run under the caller's SESSION_CONTEXT and
    // can be filtered by row-level security, so the already-resolved header is used as given.
    public override Task<IGenericResult<IGenericConnection>> Get(IConnectionImplementationConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (configuration is null)
        {
            return Task.FromResult(GenericResult<IGenericConnection>.Failure(
                ConnectionProviderLogger.ConnectionConfigurationNotFound(_logger, "(null)")));
        }

        // Why: NO FALLBACKS — the name identifies the configuration in every failure message and in
        // the stale check, so a nameless configuration cannot be served. The old code routed it to
        // base.Get(), a path that cannot build a connection at all for this domain, so that
        // "fallback" only turned a clear miss into a confusing one.
        if (string.IsNullOrWhiteSpace(configuration.Name))
        {
            return Task.FromResult(GenericResult<IGenericConnection>.Failure(
                ConnectionProviderLogger.ConnectionConfigurationNameMissing(_logger, configuration.Id.ToString())));
        }

        return CreateChecked(configuration.Name, ct => CreateFromHeader(configuration, ct), cancellationToken);
    }

    // IDataConnectionProvider — async wrappers returning IDataConnection

    async Task<IGenericResult<IDataConnection>> IDataConnectionProvider.Get(string name, CancellationToken cancellationToken)
        => Cast<IDataConnection>(await Get(name, cancellationToken).ConfigureAwait(false));

    // Why: routes through the virtual IGenericConfiguration overload so the typed override above
    // handles ConnectionConfiguration instances.
    async Task<IGenericResult<IDataConnection>> IDataConnectionProvider.Get(IGenericConfiguration configuration, CancellationToken cancellationToken)
        => Cast<IDataConnection>(await Get(configuration, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<IDataConnection>> IDataConnectionProvider.Get(Guid id, CancellationToken cancellationToken)
        => Cast<IDataConnection>(await Get(id, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<IReadOnlyList<IDataConnection>>> IDataConnectionProvider.Get(CancellationToken cancellationToken)
    {
        var result = await Get(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess) return result.ToNewResult<IReadOnlyList<IDataConnection>>();
        var typed = result.Value?.OfType<IDataConnection>().ToList() ?? [];
        return GenericResult<IReadOnlyList<IDataConnection>>.Success(typed);
    }

    /// <summary>
    /// Gets a typed data connection by name.
    /// </summary>
    // Why: Explicit interface implementation used so the method constraint (T : IDataConnection) matches
    // IDataConnectionProvider.Get<T> exactly without conflicting with IPlatformServiceProvider.Get<T>
    // (T : IGenericService). Callers use IDataConnectionProvider directly.
    // Why: no separate resolution here — Get(name) IS the creation path. The old duplicate fast path
    // bypassed the staleness check, so Get<T> could hand out a connection Get(name) would reject.
    async Task<IGenericResult<T>> IDataConnectionProvider.Get<T>(string name, CancellationToken cancellationToken)
        => Cast<T>(await Get(name, cancellationToken).ConfigureAwait(false));


    // Why no cache: the provider is a name -> service lookup and holds no state. The connection object
    // it hands back wraps a driver that already pools underneath, so caching one above it bought nothing
    // and cost a disposal lifecycle, an eviction API and a staleness dance. Every call creates.
    //
    // Why the stale check stays: a connection that is stale straight out of the factory is a factory or
    // configuration defect, so it fails loud rather than being handed to a caller.
    /// <inheritdoc />
    // Why: a connection can come back already stale — the factory succeeded but what it produced is
    // unusable. Every creation path goes through here, so the check cannot be skipped by adding one.
    protected override IGenericResult<IGenericConnection> Create(
        IServiceFactory<IGenericConnection> factory,
        IConnectionImplementationConfiguration configuration)
    {
        var result = base.Create(factory, configuration);
        if (!result.IsSuccess || !result.Value!.IsStale)
            return result;

        return GenericResult<IGenericConnection>.Failure(
            ConnectionProviderLogger.ConnectionStaleOnCreation(_logger, configuration.Name));
    }

    private async Task<IGenericResult<IGenericConnection>> CreateChecked(
        string name,
        Func<CancellationToken, Task<IGenericResult<IGenericConnection>>> create,
        CancellationToken cancellationToken)
    {
        var result = await create(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || !result.Value!.IsStale)
            return result;

        return GenericResult<IGenericConnection>.Failure(
            ConnectionProviderLogger.ConnectionStaleOnCreation(_logger, name));
    }


    // Why: the header provider composes the whole aggregate — it runs PopulateTypedBody and attaches
    // the typed body as header.Configuration before returning — so resolution is ONE call, never a
    // per-type child-provider lookup. That is why the base class's CreateFromType path is bypassed
    // entirely for connections: the base's child-provider dictionary is empty here by design.

    // Why: the domain's ONLY creation step. Dispatches on the header's ServiceOptionType to the
    // registered factory, which owns secret resolution through the secret-manager provider it was
    // constructed with. Every missing prerequisite below is a DISTINCT structured failure — the three
    // used to collapse into one "no factory registered" message that named the wrong problem.
    private async Task<IGenericResult<IGenericConnection>> CreateFromHeader(IConnectionImplementationConfiguration header, CancellationToken cancellationToken)
    {
        var serviceOptionType = header.ServiceOptionType;
        if (string.IsNullOrEmpty(serviceOptionType))
        {
            return GenericResult<IGenericConnection>.Failure(
                ConnectionProviderLogger.ServiceOptionTypeMissing(_logger, header.Name));
        }

        if (header.Configuration is null)
        {
            // Why: ComposedHeaderNoConfiguration is [LoggerMessage] (void) — it logs the warning that
            // names the header; TypedBodyMissing is [MessageLogging] and carries the failure.
            ConnectionProviderLogger.ComposedHeaderNoConfiguration(_logger, header.Name, serviceOptionType);
            return GenericResult<IGenericConnection>.Failure(
                ConnectionProviderLogger.TypedBodyMissing(_logger, header.Name, serviceOptionType));
        }

        // Why the provider's own registry and not the container: each option registered its factory
        // func from its Register method, and this provider resolved every one of them in its constructor.
        // Nothing here reaches back into DI at request time.
        if (!Factories.TryGetValue(serviceOptionType, out var factory))
        {
            ConnectionProviderLogger.ComposedHeaderNoFactory(_logger, serviceOptionType, header.Name);

            // Why the registry contents travel with the miss: "no factory for 'MsSql'" cannot
            // distinguish an empty registry from one holding a different discriminator, and those
            // have opposite causes. Printing what IS registered answers that in the same line.
            ServiceLogger.FactoryLookupMiss(
                _logger,
                GetType().Name,
                serviceOptionType,
                header.Name,
                Factories.Count == 0 ? "<empty>" : string.Join(", ", Factories.Keys));

            return GenericResult<IGenericConnection>.Failure(
                ServiceLogger.NoServiceTypeForOption(_logger, serviceOptionType, header.Name));
        }

        // Why: the registration dictionary is typed IServiceFactory<IGenericConnection>, which has only
        // the sync pure-construction Create. The async, secret-aware Create lives on IConnectionFactory,
        // so the cast is still the gate that proves this domain's creation contract is implemented.
        if (factory is not IConnectionFactory connectionFactory)
        {
            return GenericResult<IGenericConnection>.Failure(
                ConnectionProviderLogger.FactoryNotConnectionFactory(_logger, header.Name, serviceOptionType));
        }

        ConnectionProviderLogger.ComposedHeaderCreating(_logger, header.Name, serviceOptionType, header.Configuration.GetType().Name);

        // Why the header and not header.Configuration: a factory needs the connection's NAME, and the
        // name is on the header — the typed body's table has no Name column. Every connection factory
        // unwraps a composed header to (typed body, header.Name) and treats a bare typed body as
        // nameless. Handing over the body alone therefore produced a connection that could not say what
        // it was, which stayed invisible only while the name had a fallback to substitute.
        var result = await connectionFactory.Create(header, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
            ConnectionProviderLogger.ComposedHeaderCreated(_logger, header.Name, serviceOptionType);
        else
            ConnectionProviderLogger.ConnectionCreationFailed(_logger, serviceOptionType, result.CurrentMessage ?? "factory.Create returned failure");

        return result;
    }

}
