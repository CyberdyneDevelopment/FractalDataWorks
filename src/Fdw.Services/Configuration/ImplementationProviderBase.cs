using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Conventions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Configuration;

/// <summary>
/// Supplies and persists one implementation's own configuration.
/// </summary>
/// <typeparam name="TConfiguration">The implementation this provider reads and writes.</typeparam>
/// <remarks>
/// An implementation deals with exactly one kind of configuration; that is what makes it an
/// implementation rather than a domain. Its rows are always reached through the domain that owns
/// them — the caller passes the domain row's durable Id, and the join to the domain's RowId happens
/// inside the query, so no RowId is ever materialised here. There is no case where an
/// implementation has no domain, and so nothing to branch on.
/// <para>
/// A derived provider supplies a name, a data store, a schema and a table, and nothing else.
/// </para>
/// </remarks>
public abstract class ImplementationProviderBase<TConfiguration>
    : IImplementationConfigurationProvider,
      IImplementationConfigurationProvider<TConfiguration>
    where TConfiguration : class, IImplementationConfiguration, new()
{
    private readonly ILogger<ImplementationProviderBase<TConfiguration>> _logger;
    private readonly IConfigurationGatewayProvider _gatewayProvider;
    private readonly ImplementationConfigurationCommand<TConfiguration> _commands;

    /// <summary>Gets the configuration connection this implementation's rows live on.</summary>
    public string DataStoreName { get; private set; }

    /// <summary>Gets the schema this implementation's rows live in.</summary>
    public string PathName { get; private set; }

    /// <summary>Initializes the provider.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto <paramref name="dataStoreName"/>.</param>
    /// <param name="dataStoreName">The configuration connection the rows live on.</param>
    /// <param name="pathName">The schema the rows live in.</param>
    /// <param name="tableName">The implementation table.</param>
    protected ImplementationProviderBase(
        ILogger<ImplementationProviderBase<TConfiguration>>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string tableName)
    {
        _logger = logger ?? NullLogger<ImplementationProviderBase<TConfiguration>>.Instance;
        _gatewayProvider = gatewayProvider ?? throw new ArgumentNullException(nameof(gatewayProvider));
        DataStoreName = dataStoreName ?? throw new ArgumentNullException(nameof(dataStoreName));
        PathName = pathName ?? throw new ArgumentNullException(nameof(pathName));
        _commands = new ImplementationConfigurationCommand<TConfiguration>(
            tableName ?? throw new ArgumentNullException(nameof(tableName)));
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
    public async Task<IGenericResult<TConfiguration>> Get(Guid domainId, CancellationToken cancellationToken = default)
    {
        var rows = await Read(domainId, null, cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess
            ? GenericResult<TConfiguration>.Success(rows.Value?.FirstOrDefault()!)
            : rows.ToNewResult<TConfiguration>();
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<TConfiguration>> Get(Guid domainId, DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        var rows = await Read(domainId, asOf, cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess
            ? GenericResult<TConfiguration>.Success(rows.Value?.FirstOrDefault()!)
            : rows.ToNewResult<TConfiguration>();
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<TConfiguration>>> Get(
        IEnumerable<Guid> domainIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainIds);

        var found = new List<TConfiguration>();
        foreach (var domainId in domainIds)
        {
            var one = await Get(domainId, cancellationToken).ConfigureAwait(false);
            if (!one.IsSuccess) return one.ToNewResult<IReadOnlyList<TConfiguration>>();
            if (one.Value is not null) found.Add(one.Value);
        }

        return GenericResult<IReadOnlyList<TConfiguration>>.Success(found);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<TConfiguration>>> Get(CancellationToken cancellationToken = default)
    {
        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<IReadOnlyList<TConfiguration>>();

        var rows = await gateway.Value!.Execute<IEnumerable<TConfiguration>>(
            _commands.List(DataStoreName, PathName), Target, cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess
            ? GenericResult<IReadOnlyList<TConfiguration>>.Success([.. rows.Value ?? []])
            : rows.ToNewResult<IReadOnlyList<TConfiguration>>();
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<TConfiguration>>> Find(
        Func<TConfiguration, bool> predicate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var all = await Get(cancellationToken).ConfigureAwait(false);
        if (!all.IsSuccess) return all;

        var matched = new List<TConfiguration>();
        foreach (var one in all.Value ?? [])
        {
            if (predicate(one)) matched.Add(one);
        }

        return GenericResult<IReadOnlyList<TConfiguration>>.Success(matched);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<TConfiguration>> Save(TConfiguration record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<TConfiguration>();

        var written = await gateway.Value!.Execute<TConfiguration>(
            _commands.Create(DataStoreName, PathName, record), Target, cancellationToken).ConfigureAwait(false);
        return written.IsSuccess ? GenericResult<TConfiguration>.Success(record) : written;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Delete(Guid domainId, CancellationToken cancellationToken = default)
    {
        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<TConfiguration>();

        return await gateway.Value!.Execute<TConfiguration>(
            _commands.Delete(DataStoreName, PathName, domainId), Target, cancellationToken).ConfigureAwait(false);
    }

    // An implementation row is always reached through its domain: the id is the DOMAIN row's, and
    // the join resolves it to this row on the domain's RowId. There is no implementation without a
    // domain, so there is no other way in and nothing to decide.
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // resolving the domain join is one decision
    private async Task<IGenericResult<IEnumerable<TConfiguration>>> Read(
        Guid domainId, DateTimeOffset? asOf, CancellationToken ct)
    {
        if (domainId == Guid.Empty)
            return GenericResult<IEnumerable<TConfiguration>>.Success([]);

        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<IEnumerable<TConfiguration>>();

        var container = gateway.Value!.DataStores
            .FirstOrDefault(s => string.Equals(s.Name, DataStoreName, StringComparison.Ordinal))?
            .Path(PathName);
        if (container is null || !container.IsSuccess || container.Value is null)
            return GenericResult<IEnumerable<TConfiguration>>.Failure(
                DefaultConfigurationProviderLog.ContainerNotFoundInStore(_logger, typeof(TConfiguration).Name, _commands.TableName, DataStoreName));

        var own = container.Value.Container(_commands.TableName);
        if (!own.IsSuccess || own.Value is null)
            return GenericResult<IEnumerable<TConfiguration>>.Failure(
                DefaultConfigurationProviderLog.ContainerNotFoundInStore(_logger, typeof(TConfiguration).Name, _commands.TableName, DataStoreName));

        var fk = ForeignKeyToDomain(own.Value.Keys, container.Value);
        if (fk?.ReferencedContainer is null || fk.KeyFields.Count == 0)
            return GenericResult<IEnumerable<TConfiguration>>.Failure(
                DefaultConfigurationProviderLog.NoSuitableKeyForContainer(
                    _logger, typeof(TConfiguration).Name, _commands.TableName));

        var domain = fk.ReferencedContainer;
        var domainKeys = domain.Keys.Count > 0
            ? domain.Keys
            : container.Value.Container(domain.Name) is { IsSuccess: true, Value: not null } resolved
                ? resolved.Value.Keys
                : domain.Keys;

        var joinColumn = KeyField(domainKeys, "Physical");   // the domain's RowId — what the FK points at
        var keyColumn = KeyField(domainKeys, "Logical");     // the domain's durable Id — what filters
        if (joinColumn is null || keyColumn is null)
            return GenericResult<IEnumerable<TConfiguration>>.Failure(
                DefaultConfigurationProviderLog.NoSuitableKeyForContainer(_logger, typeof(TConfiguration).Name, domain.Name));

        return await gateway.Value!.Execute<IEnumerable<TConfiguration>>(
            _commands.GetByParentJoin(
                DataStoreName, PathName,
                fk.KeyFields[0].LocalField.Name, domain.Name, joinColumn, keyColumn, domainId, asOf),
            Target, ct).ConfigureAwait(false);
    }

    private static IContainerKey? ForeignKeyToDomain(IReadOnlyList<IContainerKey> keys, IDataNodePath path)
    {
        for (var i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            if (!string.Equals(key.KeyType.Name, "Foreign", StringComparison.Ordinal)) continue;
            if (key.KeyFields.Count == 0) continue;

            var col = key.KeyFields[0].LocalField.Name;
            if (!col.EndsWith("RowId", StringComparison.Ordinal)) continue;
            if (string.Equals(col, "TenantRowId", StringComparison.Ordinal)) continue;
            if (string.Equals(col, "VisibilityGroupRowId", StringComparison.Ordinal)) continue;
            if (key.ReferencedContainer?.Name is not { } referenced) continue;
            if (!path.Container(referenced).IsSuccess) continue;

            return key;
        }

        return null;
    }

    private static string? KeyField(IReadOnlyList<IContainerKey> keys, string keyTypeName)
    {
        for (var i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            if (!string.Equals(key.KeyType.Name, keyTypeName, StringComparison.Ordinal)) continue;
            if (string.Equals(keyTypeName, "Logical", StringComparison.Ordinal) && key.ReferencedContainer is not null) continue;
            if (key.KeyFields.Count == 0) continue;
            return key.KeyFields[0].LocalField.Name;
        }

        return null;
    }

    // ── the erased surface ──────────────────────────────────────────────────

    async Task<IGenericResult<IImplementationConfiguration>> IImplementationConfigurationProvider.Get(
        Guid id, CancellationToken ct)
    {
        var found = await Get(id, ct).ConfigureAwait(false);
        return found.IsSuccess
            ? GenericResult<IImplementationConfiguration>.Success(found.Value!)
            : found.ToNewResult<IImplementationConfiguration>();
    }

    async Task<IGenericResult<IImplementationConfiguration>> IImplementationConfigurationProvider.Get(
        string name, CancellationToken ct)
    {
        var all = await Get(ct).ConfigureAwait(false);
        if (!all.IsSuccess) return all.ToNewResult<IImplementationConfiguration>();

        foreach (var one in all.Value ?? [])
        {
            if (string.Equals(one.Name, name, StringComparison.OrdinalIgnoreCase))
                return GenericResult<IImplementationConfiguration>.Success(one);
        }

        return GenericResult<IImplementationConfiguration>.Success(default!);
    }

    async Task<IGenericResult> IImplementationConfigurationProvider.Save(
        IImplementationConfiguration implementationConfiguration,
        string domain, string implementationName, string name, CancellationToken ct)
        => implementationConfiguration is TConfiguration typed
            ? await Save(typed, ct).ConfigureAwait(false)
            : GenericResult.Failure(
                DefaultConfigurationProviderLog.UntypedSaveTypeMismatch(
                    _logger, typeof(TConfiguration).Name, implementationConfiguration?.GetType().Name ?? "(null)"));

    async Task<IGenericResult> IImplementationConfigurationProvider.Delete(Guid id, CancellationToken ct)
        => await Delete(id, ct).ConfigureAwait(false);

    async Task<IGenericResult> IImplementationConfigurationProvider.Delete(string name, CancellationToken ct)
    {
        var all = await Get(ct).ConfigureAwait(false);
        if (!all.IsSuccess) return all;

        foreach (var one in all.Value ?? [])
        {
            if (string.Equals(one.Name, name, StringComparison.OrdinalIgnoreCase))
                return await Delete(one.Id, ct).ConfigureAwait(false);
        }

        return GenericResult.Success();
    }

    private DataStoreTarget Target => new(DataStoreName, PathName, _commands.TableName);
}
