using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Conventions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
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
/// <typeparam name="TContract">
/// The domain's implementation contract. A domain registers its implementations by this, so it is
/// what lets one domain hold every provider it has.
/// </typeparam>
/// <remarks>
/// An implementation deals with exactly one kind of configuration; that is what makes it an
/// implementation rather than a domain. Its rows are always reached through the domain that owns
/// them — the caller passes the domain row's durable Id, and the join to the domain's RowId happens
/// inside the query, so no RowId is ever materialised here. There is no case where an
/// implementation has no domain, and so nothing to branch on.
/// <para>
/// A derived provider supplies a name, a data store, a schema and a table, and nothing else.
/// </para>
/// <para>
/// The implementation's children hang from its row. A read loads them, a save re-saves them and a
/// delete retires them, so the provider hands back and takes the whole aggregate.
/// </para>
/// </remarks>
public abstract class ImplementationProviderBase<TConfiguration, TContract>
    : IImplementationConfigurationProvider,
      IImplementationConfigurationProvider<TContract>
    where TConfiguration : class, TContract, new()
    where TContract : IImplementationConfiguration
{
    private readonly ILogger<ImplementationProviderBase<TConfiguration, TContract>> _logger;
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
        ILogger<ImplementationProviderBase<TConfiguration, TContract>>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string tableName)
    {
        _logger = logger ?? NullLogger<ImplementationProviderBase<TConfiguration, TContract>>.Instance;
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
        if (!rows.IsSuccess) return rows.ToNewResult<IReadOnlyList<TConfiguration>>();

        return await Compose(rows.Value, null, cancellationToken).ConfigureAwait(false);
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
        if (!written.IsSuccess) return written;

        var cascade = await CascadeCollections(
            record, StripConfigurationSuffix(record.GetType().Name) + "Id", record.Id, cancellationToken).ConfigureAwait(false);
        return cascade.IsSuccess
            ? GenericResult<TConfiguration>.Success(record)
            : cascade.ToNewResult<TConfiguration>();
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Delete(Guid domainId, CancellationToken cancellationToken = default)
    {
        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<TConfiguration>();

        // The children go first, deepest first, then the row they hang from.
        var existing = await Read(domainId, null, cancellationToken).ConfigureAwait(false);
        if (!existing.IsSuccess) return existing;

        foreach (var row in existing.Value!)
        {
            var retired = await RetireCollections(
                row, StripConfigurationSuffix(row.GetType().Name) + "Id", row.Id, cancellationToken).ConfigureAwait(false);
            if (!retired.IsSuccess) return retired;
        }

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
        var domainKeys = domain.Keys;
        if (domainKeys.Count == 0)
        {
            var resolved = container.Value.Container(domain.Name);
            if (!resolved.IsSuccess)
                return resolved.ToNewResult<IEnumerable<TConfiguration>>();

            if (resolved.Value is not null)
                domainKeys = resolved.Value.Keys;
        }

        var joinColumn = KeyField(domainKeys, "Physical");   // the domain's RowId — what the FK points at
        var keyColumn = KeyField(domainKeys, "Logical");     // the domain's durable Id — what filters
        if (joinColumn is null || keyColumn is null)
            return GenericResult<IEnumerable<TConfiguration>>.Failure(
                DefaultConfigurationProviderLog.NoSuitableKeyForContainer(_logger, typeof(TConfiguration).Name, domain.Name));

        var rows = await gateway.Value!.Execute<IEnumerable<TConfiguration>>(
            _commands.GetByParentJoin(
                DataStoreName, PathName,
                fk.KeyFields[0].LocalField.Name, domain.Name, joinColumn, keyColumn, domainId, asOf),
            Target, ct).ConfigureAwait(false);
        if (!rows.IsSuccess) return rows;

        var composed = await Compose(rows.Value, asOf, ct).ConfigureAwait(false);
        return composed.IsSuccess
            ? GenericResult<IEnumerable<TConfiguration>>.Success(composed.Value!)
            : composed.ToNewResult<IEnumerable<TConfiguration>>();
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

    // ── the domain's contract ───────────────────────────────────────────────
    // What a domain registers this under. Explicit where the contract shares a signature with the
    // typed surface but not its return type; the reads hand back the same instance, and the one write
    // narrows the contract to the configuration this provider writes, or says why it cannot.

    async Task<IGenericResult<TContract>> IImplementationConfigurationProvider<TContract>.Get(
        Guid domainId, CancellationToken cancellationToken)
    {
        var found = await Get(domainId, cancellationToken).ConfigureAwait(false);
        return found.IsSuccess
            ? GenericResult<TContract>.Success(found.Value!)
            : found.ToNewResult<TContract>();
    }

    async Task<IGenericResult<TContract>> IImplementationConfigurationProvider<TContract>.Get(
        Guid domainId, DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        var found = await Get(domainId, asOf, cancellationToken).ConfigureAwait(false);
        return found.IsSuccess
            ? GenericResult<TContract>.Success(found.Value!)
            : found.ToNewResult<TContract>();
    }

    async Task<IGenericResult<IReadOnlyList<TContract>>> IImplementationConfigurationProvider<TContract>.Get(
        IEnumerable<Guid> domainIds, CancellationToken cancellationToken)
    {
        var found = await Get(domainIds, cancellationToken).ConfigureAwait(false);
        return found.IsSuccess
            ? GenericResult<IReadOnlyList<TContract>>.Success(found.Value!)
            : found.ToNewResult<IReadOnlyList<TContract>>();
    }

    async Task<IGenericResult<IReadOnlyList<TContract>>> IImplementationConfigurationProvider<TContract>.Get(
        CancellationToken cancellationToken)
    {
        var found = await Get(cancellationToken).ConfigureAwait(false);
        return found.IsSuccess
            ? GenericResult<IReadOnlyList<TContract>>.Success(found.Value!)
            : found.ToNewResult<IReadOnlyList<TContract>>();
    }

    async Task<IGenericResult<IReadOnlyList<TContract>>> IImplementationConfigurationProvider<TContract>.Find(
        Func<TContract, bool> predicate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        var found = await Find(c => predicate(c), cancellationToken).ConfigureAwait(false);
        return found.IsSuccess
            ? GenericResult<IReadOnlyList<TContract>>.Success(found.Value!)
            : found.ToNewResult<IReadOnlyList<TContract>>();
    }

    async Task<IGenericResult<TContract>> IImplementationConfigurationProvider<TContract>.Save(
        TContract record, CancellationToken cancellationToken)
    {
        if (record is not TConfiguration typed)
        {
            return GenericResult<TContract>.Failure(
                DefaultConfigurationProviderLog.UntypedSaveTypeMismatch(
                    _logger, typeof(TConfiguration).Name, record?.GetType().Name ?? "(null)"));
        }

        var saved = await Save(typed, cancellationToken).ConfigureAwait(false);
        return saved.IsSuccess
            ? GenericResult<TContract>.Success(saved.Value!)
            : saved.ToNewResult<TContract>();
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

    // ── the child cascade ───────────────────────────────────────────────────
    // An implementation's children hang from its row. The generated mapper describes each child
    // collection -- its container and the physical {Owner}RowId column on it -- so a read joins the
    // child rows to this row on RowId and recurses, and any N-level aggregate composes from data rows
    // alone (DataStore→Paths→Containers→Fields, DataSet→Fields/Sources, EscalationPolicy→Levels). A
    // write re-saves every child with its logical {Owner}Id set; the save translator resolves the
    // physical RowId from it.

    private async Task<IGenericResult<IReadOnlyList<TConfiguration>>> Compose(
        IEnumerable<TConfiguration>? rows, DateTimeOffset? asOf, CancellationToken ct)
    {
        var composed = new List<TConfiguration>();
        if (rows is null)
            return GenericResult<IReadOnlyList<TConfiguration>>.Success(composed);

        var mapper = PocoMapperCollection.ByName(typeof(TConfiguration).Name);
        foreach (var row in rows)
        {
            if (mapper == PocoMapperCollection.NotFound)
                return GenericResult<IReadOnlyList<TConfiguration>>.Failure(
                    DefaultConfigurationProviderLog.DetachMapperMissing(_logger, typeof(TConfiguration).Name));

            // The gateway owns its cached rows. Domain composition replaces Id with the domain Id;
            // doing that on the cached implementation makes later child joins use the wrong owner Id.
            var detached = new TConfiguration();
            foreach (var parameter in mapper.MapToParameters(row))
                mapper.SetValue(detached, parameter.Key, parameter.Value);

            await LoadChildrenInto(detached, mapper, _commands.TableName, asOf, ct).ConfigureAwait(false);
            composed.Add(detached);
        }

        return GenericResult<IReadOnlyList<TConfiguration>>.Success(composed);
    }

    private async Task LoadChildrenInto(object ownerRow, IPocoMapper ownerMapper, string ownerContainerName, DateTimeOffset? asOf, CancellationToken ct)
    {
        var descriptors = ownerMapper.CascadeChildren;
        if (descriptors.Count == 0)
            return;

        if (!ownerMapper.MapToParameters(ownerRow).TryGetValue("Id", out var idObj) ||
            idObj is not Guid ownerId || ownerId == Guid.Empty)
            return;

        var keys = ResolveOwnerKeyColumns(ownerContainerName);
        if (keys is null)
        {
            DefaultConfigurationProviderLog.NoSuitableKeyForContainer(_logger, ownerRow.GetType().Name, ownerContainerName);
            return;
        }

        for (var i = 0; i < descriptors.Count; i++)
            await LoadChild(ownerRow, ownerContainerName, keys.Value.Physical, keys.Value.Logical, ownerId, descriptors[i], asOf, ct).ConfigureAwait(false);
    }

    private async Task LoadChild(object ownerRow, string ownerContainer, string ownerPhysicalCol, string ownerLogicalCol, Guid ownerId, IChildCascadeDescriptor descriptor, DateTimeOffset? asOf, CancellationToken ct)
    {
        var fkColumn = descriptor.ChildForeignKeyColumn;
        if (string.IsNullOrEmpty(fkColumn))
            return;

        if (descriptor.IsPropertyCollection)
            await LoadKvpChild(ownerRow, descriptor, ownerContainer, ownerPhysicalCol, ownerLogicalCol, ownerId, fkColumn, asOf, ct).ConfigureAwait(false);
        else
            await LoadTypedListChild(ownerRow, descriptor, ownerContainer, ownerPhysicalCol, ownerLogicalCol, ownerId, fkColumn, asOf, ct).ConfigureAwait(false);
    }

    private async Task LoadKvpChild(
        object ownerRow,
        IChildCascadeDescriptor descriptor,
        string ownerContainer,
        string ownerPhysicalCol,
        string ownerLogicalCol,
        Guid ownerId,
        string fkColumn,
        DateTimeOffset? asOf,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(descriptor.ChildContainerName))
            return;

        var cmd = BuildChildJoinQuery(descriptor.ChildContainerName, fkColumn, ownerContainer, ownerPhysicalCol, ownerLogicalCol, ownerId, asOf);
        var target = new DataStoreTarget(DataStoreName, PathName, descriptor.ChildContainerName);
        var gateway = Gateway();
        if (gateway.IsFailure)
            return;

        var kvpResult = await gateway.Value!.Execute<IEnumerable<KeyValueRow>>(cmd, target, ct).ConfigureAwait(false);
        if (!kvpResult.IsSuccess || kvpResult.Value is null)
            return;

        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var kvp in kvpResult.Value)
        {
            if (!string.IsNullOrEmpty(kvp.Name))
                values[kvp.Name] = kvp.Value;
        }

        descriptor.FillDictionary(ownerRow, values);
    }

    private async Task LoadTypedListChild(
        object ownerRow,
        IChildCascadeDescriptor descriptor,
        string ownerContainer,
        string ownerPhysicalCol,
        string ownerLogicalCol,
        Guid ownerId,
        string fkColumn,
        DateTimeOffset? asOf,
        CancellationToken ct)
    {
        var childMapper = PocoMapperCollection.ByName(descriptor.ChildTypeName);
        if (childMapper == PocoMapperCollection.NotFound)
            return;

        // Why the descriptor's container name and not the child's type name: the container a
        // child's rows live in is declared in configurationSchema.json under a name the mapper
        // already carries, and it is the type name minus the Configuration suffix -- rows of
        // EscalationLevelConfiguration live in EscalationLevel. Naming the container after the
        // TYPE loaded the first level (the gateway is handed the row type as well) and then
        // broke the recursion: the nested call looked the owner container up by a name the
        // schema does not contain, found no keys, and returned without loading the grandchildren.
        var childContainerName = descriptor.ChildContainerName;
        if (string.IsNullOrEmpty(childContainerName))
            return;

        if (ChildContainerLacksColumn(childContainerName, fkColumn))
        {
            DefaultConfigurationProviderLog.ChildBindingSkippedNoDescriptor(
                _logger, descriptor.BoundPropertyName, descriptor.ChildTypeName, ownerRow.GetType().Name);
            return;
        }

        DefaultConfigurationProviderLog.ChildReadStarting(_logger, DataStoreName, PathName, childContainerName, ownerContainer, ownerId.ToString(), fkColumn);
        var cmd = BuildChildJoinQuery(childContainerName, fkColumn, ownerContainer, ownerPhysicalCol, ownerLogicalCol, ownerId, asOf);
        var target = new DataStoreTarget(DataStoreName, PathName, childContainerName);
        var gateway = Gateway();
        if (gateway.IsFailure)
            return;

        var rowsResult = await gateway.Value!.Execute(cmd, target, descriptor.ChildType, ct).ConfigureAwait(false);
        if (!rowsResult.IsSuccess || rowsResult.Value is null)
            return;

        var typedList = childMapper.CreateList();
        foreach (var item in rowsResult.Value)
            typedList.Add(item);

        // Compose the grandchildren BEFORE handing the list to the owner. The owner row is very
        // often a shared instance -- the gateway caches query results in an IMemoryCache, which
        // returns the same object to every caller -- so anything reachable from it is reachable
        // by another request mid-composition. Publishing first and filling after made a freshly
        // read row visible in its empty state: for the AuthDb store that meant a path with no
        // containers, so the RevokedAccessToken lookup failed, the revocation check failed, and
        // LocalKeyAuthenticationHandler refused a valid token. Five concurrent authenticated
        // requests reliably lost four of them (API-164); serial ones always passed, because
        // nobody else was looking during the window.
        //
        // Depth-first then one assignment closes it: the collection goes from complete to
        // complete, never through empty. It does not make the cached aggregate safe to MUTATE
        // concurrently -- two composers still race to assign -- but both now assign a whole
        // answer, and either is correct.
        foreach (var item in typedList)
        {
            if (item is not null)
                await LoadChildrenInto(item, childMapper, childContainerName, asOf, ct).ConfigureAwait(false);
        }

        descriptor.SetCollection(ownerRow, typedList);
        DefaultConfigurationProviderLog.ChildReadCompleted(_logger, childContainerName, ownerId.ToString(), typedList.Count);
    }

    /// <summary>Builds the query that loads one child collection of an implementation row.</summary>
    /// <remarks>
    /// Filters the child's own IsCurrent and IsDeleted as well as the owner's. Filtering only the
    /// owner composed every retired version and every deleted row of the child into the collection:
    /// a detached dataverse resource stayed on the map, and readiness kept counting it (FDW-795).
    /// </remarks>
    protected IDataCommand BuildChildJoinQuery(
        string childContainer,
        string fkColumn,
        string ownerContainer,
        string ownerPhysicalCol,
        string ownerLogicalCol,
        Guid ownerId,
        DateTimeOffset? asOf)
    {
        var builder = new QueryCommandBuilder<object>(DataStoreName, PathName, childContainer)
            .Join(ownerContainer, fkColumn, ownerPhysicalCol);

        builder = asOf is null
            ? builder.Where(string.Concat(ownerContainer, ".IsCurrent"), true)
            : builder
                .Where(string.Concat(ownerContainer, ".EffectiveStart"), FilterOperators.ByName("LessThanOrEqual"), asOf.Value)
                .BeginOrGroup()
                    .Where(string.Concat(ownerContainer, ".EffectiveEnd"), FilterOperators.ByName("GreaterThan"), asOf.Value)
                    .Where(string.Concat(ownerContainer, ".EffectiveEnd"), FilterOperators.ByName("IsNull"), null)
                .EndGroup();

        // The child is versioned exactly as its owner is: only its current row is part of the
        // composed collection, and a deleted child is not part of it at any time.
        if (asOf is null)
            builder = builder.Where(string.Concat(childContainer, ".IsCurrent"), true);

        return builder
            .Where(string.Concat(childContainer, ".IsDeleted"), false)
            .Where(string.Concat(ownerContainer, ".IsDeleted"), false)
            .Where(string.Concat(ownerContainer, ".", ownerLogicalCol), ownerId)
            .Build().Command;
    }

    private bool ChildContainerLacksColumn(string childContainerName, string fkColumn)
    {
        if (string.IsNullOrEmpty(fkColumn))
            return false;

        var gateway = Gateway();
        if (gateway.IsFailure)
            return false;

        var stores = gateway.Value!.DataStores;
        IDataStore? store = null;
        for (var i = 0; i < stores.Count; i++)
        {
            if (string.Equals(stores[i].Name, DataStoreName, StringComparison.Ordinal))
            {
                store = stores[i];
                break;
            }
        }
        if (store is null)
            return false;

        var pathResult = store.Path(PathName);
        if (!pathResult.IsSuccess || pathResult.Value is null)
            return false;

        var containerResult = pathResult.Value.Container(childContainerName);
        if (!containerResult.IsSuccess || containerResult.Value is null)
            return false;

        // Why: .Schema forces IField projection, which no current connection-type builder
        // populates (SQL included) -- .Nodes gives the field names this check actually needs.
        var fields = containerResult.Value.Nodes;
        if (fields.Count == 0)
            return false;

        for (var i = 0; i < fields.Count; i++)
        {
            if (string.Equals(fields[i].Name, fkColumn, StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    private (string Physical, string Logical)? ResolveOwnerKeyColumns(string containerName)
    {
        var gateway = Gateway();
        if (gateway.IsFailure)
            return null;

        var stores = gateway.Value!.DataStores;
        IDataStore? store = null;
        for (var i = 0; i < stores.Count; i++)
        {
            if (string.Equals(stores[i].Name, DataStoreName, StringComparison.Ordinal))
            {
                store = stores[i];
                break;
            }
        }
        if (store is null)
            return null;

        var pathResult = store.Path(PathName);
        if (!pathResult.IsSuccess || pathResult.Value is null)
            return null;

        var containerResult = pathResult.Value.Container(containerName);
        if (!containerResult.IsSuccess || containerResult.Value is null)
            return null;

        var physical = KeyField(containerResult.Value.Keys, "Physical");
        var logical = KeyField(containerResult.Value.Keys, "Logical");
        return physical is null || logical is null ? null : (physical, logical);
    }

    private async Task<IGenericResult> CascadeCollections(
        IGenericConfiguration owner,
        string fkName,
        Guid fkValue,
        CancellationToken ct)
    {
        var mapper = PocoMapperCollection.ByName(owner.GetType().Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult.Success();

        var descriptors = mapper.CascadeChildren;
        for (var c = 0; c < descriptors.Count; c++)
        {
            if (descriptors[c].IsPropertyCollection)
            {
                var kvpResult = await SaveKvpChild(owner, descriptors[c], fkName, fkValue, ct).ConfigureAwait(false);
                if (!kvpResult.IsSuccess) return kvpResult;
                continue;
            }
            if (descriptors[c].GetCollection(owner) is not System.Collections.IEnumerable items) continue;
            foreach (var item in items)
            {
                // Why this is logged rather than skipped quietly: a type-test `continue` treats
                // "did not match" as "nothing to do", so the row is dropped and NOTHING reports
                // it — not the build, not the save result, not an audit trail. That silence is
                // why the Dataverse children were discarded unnoticed. A rewrite at least leaves
                // evidence; a silent skip leaves none, so it has to announce itself.
                if (item is not IGenericConfiguration childCfg)
                {
                    DefaultConfigurationProviderLog.ChildSkippedNotConfiguration(
                        _logger, owner.GetType().Name, item?.GetType().Name ?? "null");
                    continue;
                }

                // Link the child row to its parent via the logical FK, set by column name through the
                // child's generated mapper — reflection-free; translator resolves the physical RowId FK
                // on insert. The FK name is runtime-varying (this row's FK for level-1, parent-item FK
                // deeper), so a generated SetValue(name) — not a fixed typed setter — is required.
                var childMapper = PocoMapperCollection.ByName(childCfg.GetType().Name);
                if (childMapper != PocoMapperCollection.NotFound)
                    childMapper.SetValue(childCfg, fkName, fkValue);

                var itemResult = await SaveOneChild(childCfg, ct).ConfigureAwait(false);
                if (!itemResult.IsSuccess) return itemResult;

                // Recurse: this child's own collections are FK'd to THIS child's logical Id.
                var nestedResult = await CascadeCollections(
                    childCfg,
                    StripConfigurationSuffix(childCfg.GetType().Name) + "Id",
                    childCfg.Id,
                    ct).ConfigureAwait(false);
                if (!nestedResult.IsSuccess) return nestedResult;
            }
        }

        return GenericResult.Success();
    }

    private static string StripConfigurationSuffix(string typeName) =>
        typeName.EndsWith("Configuration", StringComparison.Ordinal)
            ? typeName[..^"Configuration".Length]
            : typeName;

    // Saves a single composed child via its per-type ConfigurationCommand, looked up by config-TYPE
    // identity with no reflection (the non-generic IConfigurationCommands.Create / IConfigurationGateway.Execute).
    private async Task<IGenericResult> SaveOneChild(IGenericConfiguration childCfg, CancellationToken ct)
    {
        var childType = childCfg.GetType();
        var command = ConfigurationCommands.All().FirstOrDefault(c => c.ConfigType == childType);
        if (command is null)
        {
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.NoChildCommandForType(
                    _logger, typeof(TConfiguration).Name, childType.Name));
        }

        if (childCfg.Id == Guid.Empty)
            childCfg.Id = Guid.CreateVersion7();

        var saveCmd = command.Create(DataStoreName, PathName, childCfg);
        var childTarget = new DataStoreTarget(DataStoreName, PathName, command.ContainerName);

        // Non-generic IConfigurationGateway.Execute — the child INSERT returns no materialized value and
        // its type is only known at runtime, so it cannot close Execute<T> without reflection.
        var gateway = Gateway();
        if (gateway.IsFailure) return gateway;

        return await gateway.Value!.Execute(saveCmd, childTarget, ct).ConfigureAwait(false);
    }

    private async Task<IGenericResult> SaveKvpChild(
        IGenericConfiguration owner,
        IChildCascadeDescriptor descriptor,
        string fkName,
        Guid fkValue,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(descriptor.ChildContainerName))
            return GenericResult.Success();

        var bag = descriptor.ReadDictionary(owner);
        if (bag is null || bag.Count == 0)
            return GenericResult.Success();

        var target = new DataStoreTarget(DataStoreName, PathName, descriptor.ChildContainerName);
        var fk = new Dictionary<string, object?>(1, StringComparer.Ordinal) { [fkName] = fkValue };

        foreach (var entry in bag)
        {
            var saveCmd = new ConfigurationSaveCommand<KeyValueRow>(
                new KeyValueRow { Name = entry.Key, Value = entry.Value }, fk);
            var gateway = Gateway();
            if (gateway.IsFailure) return gateway;

            var result = await gateway.Value!.Execute(saveCmd, target, ct).ConfigureAwait(false);
            if (!result.IsSuccess) return result;
        }

        DefaultConfigurationProviderLog.KvpChildSaved(
            _logger, owner.GetType().Name, descriptor.ChildContainerName, bag.Count);
        return GenericResult.Success();
    }

    private async Task<IGenericResult> RetireCollections(
        IGenericConfiguration owner,
        string fkName,
        Guid fkValue,
        CancellationToken ct)
    {
        var mapper = PocoMapperCollection.ByName(owner.GetType().Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult.Success();

        var descriptors = mapper.CascadeChildren;
        for (var c = 0; c < descriptors.Count; c++)
        {
            if (descriptors[c].IsPropertyCollection)
            {
                var kvpResult = await RetireKvpChild(owner, descriptors[c], fkName, fkValue, ct).ConfigureAwait(false);
                if (!kvpResult.IsSuccess) return kvpResult;
                continue;
            }

            if (descriptors[c].GetCollection(owner) is not System.Collections.IEnumerable items) continue;
            foreach (var item in items)
            {
                // Logged, not skipped quietly -- see CascadeCollections.
                if (item is not IGenericConfiguration childCfg)
                {
                    DefaultConfigurationProviderLog.ChildSkippedNotConfiguration(
                        _logger, owner.GetType().Name, item?.GetType().Name ?? "null");
                    continue;
                }

                var nested = await RetireCollections(
                    childCfg,
                    StripConfigurationSuffix(childCfg.GetType().Name) + "Id",
                    childCfg.Id,
                    ct).ConfigureAwait(false);
                if (!nested.IsSuccess) return nested;

                var itemResult = await RetireOneChild(childCfg, ct).ConfigureAwait(false);
                if (!itemResult.IsSuccess) return itemResult;
            }
        }

        return GenericResult.Success();
    }

    // Retires a single composed child row via its per-type ConfigurationCommand — the delete mirror of
    // SaveOneChild, resolved the same way and failing loud on the same condition.
    private async Task<IGenericResult> RetireOneChild(IGenericConfiguration childCfg, CancellationToken ct)
    {
        var childType = childCfg.GetType();
        var command = ConfigurationCommands.All().FirstOrDefault(c => c.ConfigType == childType);
        if (command is null)
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.NoChildCommandForType(
                    _logger, typeof(TConfiguration).Name, childType.Name));

        var gateway = Gateway();
        if (gateway.IsFailure) return gateway;

        return await gateway.Value!.Execute(
            command.Delete(DataStoreName, PathName, childCfg.Id),
            new DataStoreTarget(DataStoreName, PathName, command.ContainerName),
            ct).ConfigureAwait(false);
    }

    private async Task<IGenericResult> RetireKvpChild(
        IGenericConfiguration owner,
        IChildCascadeDescriptor descriptor,
        string fkName,
        Guid fkValue,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(descriptor.ChildContainerName))
            return GenericResult.Success();

        var bag = descriptor.ReadDictionary(owner);
        if (bag is null || bag.Count == 0)
            return GenericResult.Success();

        var gateway = Gateway();
        if (gateway.IsFailure) return gateway;

        return await gateway.Value!.Execute(
            new ConfigurationDeleteCommand(fkValue, fkName),
            new DataStoreTarget(DataStoreName, PathName, descriptor.ChildContainerName),
            ct).ConfigureAwait(false);
    }

    private IGenericResult<IConfigurationGateway> Gateway() => _gatewayProvider.Get(DataStoreName);

    private DataStoreTarget Target => new(DataStoreName, PathName, _commands.TableName);
}
