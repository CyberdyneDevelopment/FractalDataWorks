using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Configuration;

/// <summary>
/// Supplies the configured members of one domain.
/// </summary>
/// <typeparam name="TImplementationConfiguration">The domain's implementation contract.</typeparam>
/// <remarks>
/// A domain deals with more than one kind of implementation; that is what makes it a domain. Every
/// read here is the same three steps written out: read the domain row, take the implementation it
/// names and its own Id, and pass them to the provider registered under that name. The domain
/// record itself is never returned — it is how the implementation is found, not the answer.
/// <para>
/// A derived provider supplies a name, a data store, a schema and a table, and nothing else. There
/// is no logic to put in one.
/// </para>
/// </remarks>
public abstract class DomainConfigurationProviderBase<TImplementationConfiguration>
    : IDomainConfigurationProvider,
      IDomainConfigurationProvider<TImplementationConfiguration>
    where TImplementationConfiguration : IImplementationConfiguration
{
    private readonly ILogger<DomainConfigurationProviderBase<TImplementationConfiguration>> _logger;
    private readonly IConfigurationGatewayProvider _gatewayProvider;
    private readonly DomainConfigurationCommand _commands;

    // Keyed by the value a domain row carries in its Implementation column.
    private readonly ConcurrentDictionary<string, IImplementationConfigurationProvider<TImplementationConfiguration>> _implementations
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the configuration connection this domain's rows live on.</summary>
    public string DataStoreName { get; private set; }

    /// <summary>Gets the schema this domain's rows live in.</summary>
    public string PathName { get; private set; }

    /// <summary>Initializes the provider.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto <paramref name="dataStoreName"/>.</param>
    /// <param name="dataStoreName">The configuration connection the rows live on.</param>
    /// <param name="pathName">The schema the rows live in.</param>
    /// <param name="tableName">The domain table.</param>
    protected DomainConfigurationProviderBase(
        ILogger<DomainConfigurationProviderBase<TImplementationConfiguration>>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string tableName)
    {
        _logger = logger ?? NullLogger<DomainConfigurationProviderBase<TImplementationConfiguration>>.Instance;
        _gatewayProvider = gatewayProvider ?? throw new ArgumentNullException(nameof(gatewayProvider));
        DataStoreName = dataStoreName ?? throw new ArgumentNullException(nameof(dataStoreName));
        PathName = pathName ?? throw new ArgumentNullException(nameof(pathName));
        _commands = new DomainConfigurationCommand(tableName ?? throw new ArgumentNullException(nameof(tableName)));
    }

    /// <summary>Points this provider at a different store or schema than its own default.</summary>
    /// <param name="dataStoreName">The configuration connection to read from.</param>
    /// <param name="pathName">The schema to read from.</param>
    public void SetConfiguration(string dataStoreName, string pathName)
    {
        DataStoreName = dataStoreName ?? throw new ArgumentNullException(nameof(dataStoreName));
        PathName = pathName ?? throw new ArgumentNullException(nameof(pathName));
    }

    /// <inheritdoc/>
    public IGenericResult Register<T>(string name, T implementationConfigurationProvider)
    {
        if (implementationConfigurationProvider is not IImplementationConfigurationProvider<TImplementationConfiguration> typed)
        {
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.ProviderNotErasable(
                    _logger, name, implementationConfigurationProvider?.GetType().FullName ?? "(null)"));
        }

        _implementations[name] = typed;
        DefaultConfigurationProviderLog.TypedProviderRegistered(_logger, typeof(TImplementationConfiguration).Name, name);
        return GenericResult.Success();
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<TImplementationConfiguration>> Get(string name, CancellationToken ct = default)
    {
        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<TImplementationConfiguration>();

        var rows = await gateway.Value!.Execute<IEnumerable<DomainConfiguration>>(
            _commands.Get(DataStoreName, PathName, name), Target, ct).ConfigureAwait(false);
        if (!rows.IsSuccess) return rows.ToNewResult<TImplementationConfiguration>();
        if (rows.Value?.FirstOrDefault() is not { } row)
            return GenericResult<TImplementationConfiguration>.Success(default!);

        if (row.Implementation is not { } implementation || !_implementations.TryGetValue(implementation, out var provider))
            return GenericResult<TImplementationConfiguration>.Failure(
                DefaultConfigurationProviderLog.NoImplementationProvider(_logger, row.Name, row.Implementation ?? "(none)"));

        var loaded = await provider.Get(row.Id, ct).ConfigureAwait(false);
        if (loaded is { IsSuccess: true, Value: not null })
        {
            loaded.Value.Name = row.Name;
            loaded.Value.Domain = row.Domain;
            loaded.Value.Implementation = implementation;
        }

        return loaded;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<TImplementationConfiguration>> Get(Guid id, CancellationToken ct = default)
    {
        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<TImplementationConfiguration>();

        var rows = await gateway.Value!.Execute<IEnumerable<DomainConfiguration>>(
            _commands.Get(DataStoreName, PathName, id), Target, ct).ConfigureAwait(false);
        if (!rows.IsSuccess) return rows.ToNewResult<TImplementationConfiguration>();
        if (rows.Value?.FirstOrDefault() is not { } row)
            return GenericResult<TImplementationConfiguration>.Success(default!);

        if (row.Implementation is not { } implementation || !_implementations.TryGetValue(implementation, out var provider))
            return GenericResult<TImplementationConfiguration>.Failure(
                DefaultConfigurationProviderLog.NoImplementationProvider(_logger, row.Name, row.Implementation ?? "(none)"));

        var loaded = await provider.Get(row.Id, ct).ConfigureAwait(false);
        if (loaded is { IsSuccess: true, Value: not null })
        {
            loaded.Value.Name = row.Name;
            loaded.Value.Domain = row.Domain;
            loaded.Value.Implementation = implementation;
        }

        return loaded;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<TImplementationConfiguration>> Get(Guid id, DateTimeOffset asOf, CancellationToken ct = default)
    {
        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<TImplementationConfiguration>();

        var rows = await gateway.Value!.Execute<IEnumerable<DomainConfiguration>>(
            _commands.Get(DataStoreName, PathName, id, asOf), Target, ct).ConfigureAwait(false);
        if (!rows.IsSuccess) return rows.ToNewResult<TImplementationConfiguration>();
        if (rows.Value?.FirstOrDefault() is not { } row)
            return GenericResult<TImplementationConfiguration>.Success(default!);

        if (row.Implementation is not { } implementation || !_implementations.TryGetValue(implementation, out var provider))
            return GenericResult<TImplementationConfiguration>.Failure(
                DefaultConfigurationProviderLog.NoImplementationProvider(_logger, row.Name, row.Implementation ?? "(none)"));

        // The instant reaches the implementation read too, or the answer mixes two versions.
        var loaded = await provider.Get(row.Id, asOf, ct).ConfigureAwait(false);
        if (loaded is { IsSuccess: true, Value: not null })
        {
            loaded.Value.Name = row.Name;
            loaded.Value.Domain = row.Domain;
            loaded.Value.Implementation = implementation;
        }

        return loaded;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<TImplementationConfiguration>>> Get(CancellationToken ct = default)
    {
        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<IReadOnlyList<TImplementationConfiguration>>();

        var rows = await gateway.Value!.Execute<IEnumerable<DomainConfiguration>>(
            _commands.List(DataStoreName, PathName), Target, ct).ConfigureAwait(false);
        if (!rows.IsSuccess) return rows.ToNewResult<IReadOnlyList<TImplementationConfiguration>>();

        var found = new List<TImplementationConfiguration>();
        foreach (var row in rows.Value ?? [])
        {
            if (row.Implementation is not { } implementation || !_implementations.TryGetValue(implementation, out var provider))
                return GenericResult<IReadOnlyList<TImplementationConfiguration>>.Failure(
                    DefaultConfigurationProviderLog.NoImplementationProvider(_logger, row.Name, row.Implementation ?? "(none)"));

            var loaded = await provider.Get(row.Id, ct).ConfigureAwait(false);
            if (!loaded.IsSuccess) return loaded.ToNewResult<IReadOnlyList<TImplementationConfiguration>>();
            if (loaded.Value is null) continue;

            loaded.Value.Name = row.Name;
            loaded.Value.Domain = row.Domain;
            loaded.Value.Implementation = implementation;
            found.Add(loaded.Value);
        }

        return GenericResult<IReadOnlyList<TImplementationConfiguration>>.Success(found);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<T>>> Find<T>(Func<T, bool> predicate, CancellationToken ct = default)
        where T : TImplementationConfiguration
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var all = await Get(ct).ConfigureAwait(false);
        if (!all.IsSuccess) return all.ToNewResult<IReadOnlyList<T>>();

        var matched = new List<T>();
        foreach (var one in all.Value ?? [])
        {
            if (one is T typed && predicate(typed)) matched.Add(typed);
        }

        return GenericResult<IReadOnlyList<T>>.Success(matched);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Save<T>(
        T implementationConfiguration, string domain, string implementationName, string name,
        CancellationToken ct = default)
        where T : TImplementationConfiguration
    {
        if (!_implementations.TryGetValue(implementationName, out var provider))
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.NoImplementationProvider(_logger, name, implementationName));

        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<TImplementationConfiguration>();

        var existing = await gateway.Value!.Execute<IEnumerable<DomainConfiguration>>(
            _commands.Get(DataStoreName, PathName, name), Target, ct).ConfigureAwait(false);
        if (!existing.IsSuccess) return existing.ToNewResult<TImplementationConfiguration>();

        // The name identifies the member, so a row already carrying it is the row to hang from.
        // The two rows are never written apart: a domain row naming an implementation that was not
        // written is the record that fails to compose on the next read.
        var domainId = existing.Value?.FirstOrDefault()?.Id ?? Guid.Empty;
        if (domainId == Guid.Empty)
        {
            var record = new DomainConfiguration
            {
                Id = Guid.CreateVersion7(),
                Name = name,
                Domain = domain,
                Implementation = implementationName,
            };

            var written = await gateway.Value!.Execute<DomainConfiguration>(
                _commands.Create(DataStoreName, PathName, record), Target, ct).ConfigureAwait(false);
            if (!written.IsSuccess) return written.ToNewResult<TImplementationConfiguration>();

            domainId = record.Id;
        }

        implementationConfiguration.Name = name;
        implementationConfiguration.Domain = domain;
        implementationConfiguration.Implementation = implementationName;
        implementationConfiguration.Id = domainId;
        return await provider.Save(implementationConfiguration, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Delete(Guid id, CancellationToken ct = default)
    {
        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<TImplementationConfiguration>();

        var rows = await gateway.Value!.Execute<IEnumerable<DomainConfiguration>>(
            _commands.Get(DataStoreName, PathName, id), Target, ct).ConfigureAwait(false);
        if (!rows.IsSuccess) return rows.ToNewResult<TImplementationConfiguration>();
        if (rows.Value?.FirstOrDefault() is not { } row) return GenericResult.Success();

        if (row.Implementation is not { } implementation || !_implementations.TryGetValue(implementation, out var provider))
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.NoImplementationProvider(_logger, row.Name, row.Implementation ?? "(none)"));

        // The implementation goes first: a domain row whose implementation outlived it is a row
        // that names something no longer there.
        var removed = await provider.Delete(row.Id, ct).ConfigureAwait(false);
        if (!removed.IsSuccess) return removed;

        return await gateway.Value!.Execute<DomainConfiguration>(
            _commands.Delete(DataStoreName, PathName, row.Id), Target, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Delete(string name, CancellationToken ct = default)
    {
        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<TImplementationConfiguration>();

        var rows = await gateway.Value!.Execute<IEnumerable<DomainConfiguration>>(
            _commands.Get(DataStoreName, PathName, name), Target, ct).ConfigureAwait(false);
        if (!rows.IsSuccess) return rows.ToNewResult<TImplementationConfiguration>();
        if (rows.Value?.FirstOrDefault() is not { } row) return GenericResult.Success();

        return await Delete(row.Id, ct).ConfigureAwait(false);
    }

    // ── the erased surface ──────────────────────────────────────────────────
    // Explicit only where the erased contract and the typed one share a signature but not a return
    // type. Everything else on the two contracts is satisfied by one public method.

    async Task<IGenericResult<IImplementationConfiguration>> IDomainConfigurationProvider.Get(
        string name, CancellationToken ct)
    {
        var found = await Get(name, ct).ConfigureAwait(false);
        return found.IsSuccess
            ? GenericResult<IImplementationConfiguration>.Success(found.Value!)
            : found.ToNewResult<IImplementationConfiguration>();
    }

    async Task<IGenericResult<IImplementationConfiguration>> IDomainConfigurationProvider.Get(
        Guid id, CancellationToken ct)
    {
        var found = await Get(id, ct).ConfigureAwait(false);
        return found.IsSuccess
            ? GenericResult<IImplementationConfiguration>.Success(found.Value!)
            : found.ToNewResult<IImplementationConfiguration>();
    }

    async Task<IGenericResult> IDomainConfigurationProvider.Save(
        IImplementationConfiguration implementationConfiguration,
        string domain, string implementationName, string name, CancellationToken ct)
        => implementationConfiguration is TImplementationConfiguration typed
            ? await Save(typed, domain, implementationName, name, ct).ConfigureAwait(false)
            : GenericResult.Failure(
                DefaultConfigurationProviderLog.UntypedSaveTypeMismatch(
                    _logger,
                    typeof(TImplementationConfiguration).Name,
                    implementationConfiguration?.GetType().Name ?? "(null)"));

    private DataStoreTarget Target => new(DataStoreName, PathName, _commands.TableName);
}
