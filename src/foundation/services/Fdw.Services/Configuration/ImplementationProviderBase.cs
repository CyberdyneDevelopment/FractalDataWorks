using System;
using System.Collections;
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
using Fdw.Validation.Configuration;
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
    : IImplementationConfigurationProvider<TContract>,
      IConfigurationSupersedeParticipant<TContract>
    where TConfiguration : class, TContract, new()
    where TContract : IImplementationConfiguration
{
    private readonly ILogger<ImplementationProviderBase<TConfiguration, TContract>> _logger;
    private readonly IConfigurationGatewayProvider _gatewayProvider;
    private readonly ImplementationConfigurationCommand<TConfiguration> _commands;
    private readonly ChildSetLoader _children;
    private string? _domainContainer;

    /// <summary>Gets the configuration connection this implementation's rows live on.</summary>
    public string DataStoreName { get; private set; }

    /// <summary>Gets the schema this implementation's rows live in.</summary>
    public string PathName { get; private set; }

    /// <inheritdoc/>
    public string ContainerName => _commands.TableName;

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
        _children = new ChildSetLoader(_gatewayProvider, DataStoreName, _logger, LookupContainer, ForeignKeyToOwner, ChildContainer);
    }


    /// <inheritdoc/>
    public Task<IGenericResult<TConfiguration>> Get(Guid domainId, CancellationToken cancellationToken = default)
        => Get(domainId, ConfigurationLevels.WholeAggregate, cancellationToken);

    /// <inheritdoc/>
    public async Task<IGenericResult<TConfiguration>> Get(Guid domainId, int levels, CancellationToken cancellationToken = default)
    {
        if (levels < 0)
            return GenericResult<TConfiguration>.Failure(
                DefaultConfigurationProviderLog.ReadArgumentOutOfRange(_logger, typeof(TConfiguration).Name, nameof(levels), levels));

        // Why the set read and not Read/Compose (FDW-4254): composing one owner row by row read each child table once per row above it, so a
        // datastore of 500 containers made ~2470 requests (fields, keys and key fields of every container, FDW-4252). The set read is one statement
        // per child table per level whatever the number of rows, and an owner set of one is the same cascade. It reads the current rows, which is
        // what this overload always read; an as-of read and a read inside a unit of work keep Read, which can say which version or which connection.
        // An empty id names no row, as it did on the per-row path: nothing is read and nothing is found.
        if (domainId == Guid.Empty)
        {
            DefaultConfigurationProviderLog.ImplementationNotFound(_logger, typeof(TConfiguration).Name, domainId);
            return GenericResult<TConfiguration>.Success(default!);
        }

        var set = await GetSet([domainId], levels, cancellationToken).ConfigureAwait(false);
        if (!set.IsSuccess) return set.ToNewResult<TConfiguration>();

        if (!set.Value!.TryGetValue(domainId, out var found))
        {
            DefaultConfigurationProviderLog.ImplementationNotFound(_logger, typeof(TConfiguration).Name, domainId);
            return GenericResult<TConfiguration>.Success(default!);
        }

        DefaultConfigurationProviderLog.ImplementationComposed(_logger, typeof(TConfiguration).Name, domainId);
        return GenericResult<TConfiguration>.Success(found);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<TConfiguration>> Get(Guid domainId, DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        var rows = await Read(domainId, asOf, ConfigurationLevels.WholeAggregate, cancellationToken).ConfigureAwait(false);
        if (!rows.IsSuccess) return rows.ToNewResult<TConfiguration>();

        var found = rows.Value?.FirstOrDefault();
        if (found is null)
            DefaultConfigurationProviderLog.ImplementationNotFound(_logger, typeof(TConfiguration).Name, domainId);
        else
            DefaultConfigurationProviderLog.ImplementationComposed(_logger, typeof(TConfiguration).Name, domainId);
        return GenericResult<TConfiguration>.Success(found!);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<TChild>>> Get<TOwner, TChild>(
        Guid ownerId, int levels, CancellationToken cancellationToken = default)
    {
        if (levels < 0)
            return GenericResult<IReadOnlyList<TChild>>.Failure(
                DefaultConfigurationProviderLog.ReadArgumentOutOfRange(_logger, typeof(TChild).Name, nameof(levels), levels));

        // The implementation's own contract names this provider's row; any other owner is a row in
        // its cascade, found by the configuration type its command declares.
        var ownerType = typeof(TOwner).IsAssignableFrom(typeof(TConfiguration)) ? typeof(TConfiguration) : typeof(TOwner);
        var ownerMapper = PocoMapperCollection.ByName(ownerType.Name);
        if (ownerMapper == PocoMapperCollection.NotFound)
            return GenericResult<IReadOnlyList<TChild>>.Failure(
                DefaultConfigurationProviderLog.DetachMapperMissing(_logger, ownerType.Name));

        var descriptor = ownerMapper.CascadeChildren.FirstOrDefault(d => !d.IsPropertyCollection && d.ChildType == typeof(TChild));
        if (descriptor is null)
            return GenericResult<IReadOnlyList<TChild>>.Failure(
                DefaultConfigurationProviderLog.ChildCollectionNotOnOwner(_logger, ownerType.Name, typeof(TChild).Name));

        var owner = Owner(ownerType, ownerId);
        if (owner.IsFailure) return owner.ToNewResult<IReadOnlyList<TChild>>();
        var (ownerContainer, filter) = owner.Value;

        var childContainer = ChildContainer(descriptor, ownerType.Name);
        if (childContainer.IsFailure) return childContainer.ToNewResult<IReadOnlyList<TChild>>();

        var join = ForeignKeyToOwner(ownerContainer, childContainer.Value!);
        if (join.IsFailure) return join.ToNewResult<IReadOnlyList<TChild>>();

        var items = await ReadChildRows(
            ownerType.Name, descriptor, childContainer.Value!, ownerContainer, join.Value.OwnerColumn, filter, join.Value.ChildColumn, null, levels, cancellationToken).ConfigureAwait(false);
        if (items.IsFailure) return items.ToNewResult<IReadOnlyList<TChild>>();

        DefaultConfigurationProviderLog.ChildReadCompleted(_logger, childContainer.Value!, ownerId.ToString(), items.Value!.Count);
        return GenericResult<IReadOnlyList<TChild>>.Success(items.Value.Cast<TChild>().ToList());
    }

    // The container a child read joins to and the filter that picks the owner row out of it. This
    // implementation's own row is reached the way Read reaches it: through its Foreign key to the
    // domain row, filtered on the domain's durable Id. A row further down is filtered on the Logical
    // key its own container declares.
    private IGenericResult<(string Container, OwnerFilter Filter)> Owner(Type ownerType, Guid ownerId)
    {
        if (ownerType == typeof(TConfiguration))
        {
            var join = DomainJoin();
            if (join.IsFailure) return join.ToNewResult<(string, OwnerFilter)>();
            var (foreignKey, domainContainer, joinColumn, keyColumn) = join.Value!;

            if (ownerId == Guid.Empty)
                return GenericResult<(string, OwnerFilter)>.Failure(DefaultConfigurationProviderLog.OwnerLogicalKeyEmpty(
                    _logger, ownerType.Name, domainContainer, keyColumn));

            return GenericResult<(string, OwnerFilter)>.Success((
                _commands.TableName,
                new OwnerFilter(domainContainer, keyColumn, ownerId, new DomainHop(domainContainer, foreignKey, joinColumn))));
        }

        if (ChildCommand(ownerType) is not { } command)
            return GenericResult<(string, OwnerFilter)>.Failure(
                DefaultConfigurationProviderLog.NoChildCommandForType(_logger, typeof(TConfiguration).Name, ownerType.Name));

        var keys = LookupKeys(command.ContainerName, ownerType.Name);
        if (keys.IsFailure) return keys.ToNewResult<(string, OwnerFilter)>();

        if (ownerId == Guid.Empty)
            return GenericResult<(string, OwnerFilter)>.Failure(DefaultConfigurationProviderLog.OwnerLogicalKeyEmpty(
                _logger, ownerType.Name, command.ContainerName, keys.Value.Logical));

        return GenericResult<(string, OwnerFilter)>.Success((
            command.ContainerName,
            new OwnerFilter(command.ContainerName, keys.Value.Logical, ownerId)));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// FDW-811 REVERT (2026-09-18): this used to build one batched <c>IRelationshipReadCommand</c>
    /// and hand it to <c>ConnectionBase.ExecuteRelationshipRead</c>'s connection-agnostic default,
    /// on the theory that MsSql/PostgreSql could opt into native batching while every other
    /// connection got the same default fallback "for free". In practice the fallback lived on
    /// <c>ConnectionBase</c> -- the class every connection inherits, including FileSystemConnection,
    /// which has no concept of a SQL-shaped relational batch at all -- and its row materialization
    /// used the non-coercing <c>MapFromDictionary</c> instead of the coercing
    /// <c>RecordDictionaryReader</c>/<c>MapFromReader</c> pair every other FileSystem-backed read
    /// path already used. That silently dropped or mis-typed JSON-sourced Guid/bool columns on every
    /// bulk read with cascade children (confirmed live: reference-api-internal's
    /// AuthenticationService/ResponseBuffering/ForwardedHeaders/AuthChallenge domains all read as
    /// "no row configured" despite the row existing, some via a thrown Guid-cast exception, some via
    /// a swallowed IndexOutOfRangeException that silently returned false/absent instead).
    /// Reverted to a per-id loop through the single-id <see cref="Read"/>/<see cref="Compose"/> path. FDW-4254 replaced that loop (and the
    /// single-id current read, <see cref="Get(Guid, int, CancellationToken)"/>) with <see cref="GetSet"/>: FDW-3906's set read reads through
    /// <see cref="ConfigurationRowReader"/> and the coercing reader, not the ConnectionBase default that FDW-811 broke, and it makes a statement per
    /// table per level instead of one per row. <see cref="Read"/> remains for an as-of read and a read inside a unit of work, which the set read
    /// (current rows, its own connection) does not give.
    /// </remarks>
    public async Task<IGenericResult<IReadOnlyList<TConfiguration>>> Get(
        IEnumerable<Guid> domainIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainIds);

        // FDW-4254: the ids go to the set read in one call (a statement per table per level) and come back in the order asked, those with no
        // row left out, as the per-id loop gave them. The FDW-811 fault above was in a ConnectionBase default; the set read goes through
        // ConfigurationRowReader and the coercing reader, as every other FileSystem-safe read does.
        var asked = domainIds.ToList();
        var set = await GetSet(asked, ConfigurationLevels.WholeAggregate, cancellationToken).ConfigureAwait(false);
        if (!set.IsSuccess) return set.ToNewResult<IReadOnlyList<TConfiguration>>();

        var found = new List<TConfiguration>(set.Value!.Count);
        foreach (var domainId in asked)
        {
            if (set.Value.TryGetValue(domainId, out var one)) found.Add(one);
        }

        return GenericResult<IReadOnlyList<TConfiguration>>.Success(found);
    }

    /// <inheritdoc cref="IImplementationConfigurationProvider{TContract}.GetSet"/>
    /// <remarks>
    /// The domain rows are read by their durable Ids, then the implementation rows by the physical keys of those domain rows, then each child table
    /// by the physical keys of the rows above it (see <see cref="ChildSetLoader"/>): a statement for each table at each level, a chunk of
    /// <see cref="IConfigurationGatewayProvider.KeyChunkSize"/> keys to a statement. What it reads is what <see cref="Get(Guid, int, CancellationToken)"/> reads for each id
    /// (current, not deleted, the domain row current and not deleted too), and the rows are built the same way.
    /// </remarks>
    public async Task<IGenericResult<IReadOnlyDictionary<Guid, TConfiguration>>> GetSet(
        IEnumerable<Guid> domainIds, int levels, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainIds);
        if (levels < 0)
            return GenericResult<IReadOnlyDictionary<Guid, TConfiguration>>.Failure(
                DefaultConfigurationProviderLog.ReadArgumentOutOfRange(_logger, typeof(TConfiguration).Name, nameof(levels), levels));

        if (_gatewayProvider.KeyChunkSize < 1)
            return GenericResult<IReadOnlyDictionary<Guid, TConfiguration>>.Failure(
                DefaultConfigurationProviderLog.KeyChunkSizeNotStated(_logger, _commands.TableName, _gatewayProvider.KeyChunkSize));

        var plan = SetPlanFor();
        if (plan.IsFailure) return plan.ToNewResult<IReadOnlyDictionary<Guid, TConfiguration>>();

        var wanted = domainIds.Where(id => id != Guid.Empty).Distinct().ToList();
        var found = new Dictionary<Guid, TConfiguration>();
        foreach (var chunk in wanted.Chunk(_gatewayProvider.KeyChunkSize))
        {
            var read = await ReadSet(plan.Value!, chunk, levels, cancellationToken).ConfigureAwait(false);
            if (read.IsFailure) return read.ToNewResult<IReadOnlyDictionary<Guid, TConfiguration>>();

            foreach (var pair in read.Value!)
                found[pair.Key] = pair.Value;
        }

        DefaultConfigurationProviderLog.SetComposed(_logger, typeof(TConfiguration).Name, wanted.Count, found.Count);
        return GenericResult<IReadOnlyDictionary<Guid, TConfiguration>>.Success(found);
    }

    // Everything a set read needs of the store's configuration, resolved once: the domain's container and the field that joins to it, this
    // implementation's container and its physical key, and the mapper that builds its records.
    private sealed record SetPlan(
        IDataContainer Domain, IDataContainer Own, IPocoMapper Mapper, string ForeignKey, string JoinColumn, string KeyColumn, string OwnPhysical);

    private IGenericResult<SetPlan> SetPlanFor()
    {
        var join = DomainJoin();
        if (join.IsFailure) return join.ToNewResult<SetPlan>();
        var (foreignKey, domainContainer, joinColumn, keyColumn) = join.Value!;

        var domain = LookupContainer(domainContainer);
        if (domain.IsFailure) return domain.ToNewResult<SetPlan>();

        var own = LookupContainer(_commands.TableName);
        if (own.IsFailure) return own.ToNewResult<SetPlan>();

        var physical = ConfigurationKeys.FieldOf(own.Value!.Keys, "Physical");
        if (physical is null)
            return GenericResult<SetPlan>.Failure(DefaultConfigurationProviderLog.ContainerKeyNotDeclared(_logger, own.Value.Name, "Physical"));

        var mapper = PocoMapperCollection.ByName(typeof(TConfiguration).Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult<SetPlan>.Failure(DefaultConfigurationProviderLog.DetachMapperMissing(_logger, typeof(TConfiguration).Name));

        return GenericResult<SetPlan>.Success(new SetPlan(domain.Value!, own.Value, mapper, foreignKey, joinColumn, keyColumn, physical));
    }

    private ConfigurationRowReader RowsOf(IDataContainer container)
        => new(_gatewayProvider, DataStoreName, container.Parent.Name, container.Name, _logger);

    private static FilterCondition In(string column, IReadOnlyCollection<object> keys)
        => new() { PropertyName = column, Operator = FilterOperators.ByName("In"), Value = keys.ToList() };

    // One chunk of domain Ids: their domain rows, the implementation rows hanging from those, the children of those rows. Keyed by domain Id.
    private async Task<IGenericResult<Dictionary<Guid, TConfiguration>>> ReadSet(
        SetPlan plan, Guid[] domainIds, int levels, CancellationToken ct)
    {
        var domainRows = await RowsOf(plan.Domain).All(In(plan.KeyColumn, [.. domainIds.Cast<object>()]), ct).ConfigureAwait(false);
        if (domainRows.IsFailure) return domainRows.ToNewResult<Dictionary<Guid, TConfiguration>>();

        var domainByKey = DomainsByPhysicalKey(plan, domainRows.Value!);
        if (domainByKey.IsFailure) return domainByKey.ToNewResult<Dictionary<Guid, TConfiguration>>();
        if (domainByKey.Value!.Count == 0) return GenericResult<Dictionary<Guid, TConfiguration>>.Success([]);

        var ownRows = await RowsOf(plan.Own).All(In(plan.ForeignKey, [.. domainByKey.Value.Values.Select(domain => domain.Key)]), ct).ConfigureAwait(false);
        if (ownRows.IsFailure) return ownRows.ToNewResult<Dictionary<Guid, TConfiguration>>();

        var built = _children.Materialize(plan.Mapper, plan.Own, ownRows.Value!);
        if (built.IsFailure) return built.ToNewResult<Dictionary<Guid, TConfiguration>>();

        var owned = OwnedBy(plan, domainByKey.Value, ownRows.Value!, built.Value!);
        if (owned.IsFailure) return owned.ToNewResult<Dictionary<Guid, TConfiguration>>();

        var children = await _children.Load(owned.Value!.Owners, plan.Mapper, _commands.TableName, levels, ct).ConfigureAwait(false);
        return children.IsFailure
            ? children.ToNewResult<Dictionary<Guid, TConfiguration>>()
            : GenericResult<Dictionary<Guid, TConfiguration>>.Success(owned.Value.ByDomain);
    }

    // The domain rows read, under the text of the physical key an implementation row's Foreign key holds.
    private IGenericResult<Dictionary<string, (object Key, Guid Id)>> DomainsByPhysicalKey(SetPlan plan, IReadOnlyList<ConfigurationRow> rows)
    {
        var byKey = new Dictionary<string, (object Key, Guid Id)>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            if (!row.Columns.TryGetValue(plan.JoinColumn, out var physicalKey) || physicalKey is null)
                return GenericResult<Dictionary<string, (object Key, Guid Id)>>.Failure(
                    DefaultConfigurationProviderLog.SetChildKeyUnreadable(_logger, plan.Domain.Name, plan.JoinColumn));

            if (row.Identifier(plan.KeyColumn) is not { } id)
                return GenericResult<Dictionary<string, (object Key, Guid Id)>>.Failure(
                    DefaultConfigurationProviderLog.SetChildKeyUnreadable(_logger, plan.Domain.Name, plan.KeyColumn));

            byKey[ChildSetLoader.KeyText(physicalKey)] = (physicalKey, id);
        }

        return GenericResult<Dictionary<string, (object Key, Guid Id)>>.Success(byKey);
    }

    private sealed record OwnedRows(List<ConfigurationSetOwner> Owners, Dictionary<Guid, TConfiguration> ByDomain);

    // Each implementation record with the physical key its children hang from and the domain Id that owns it.
    private IGenericResult<OwnedRows> OwnedBy(
        SetPlan plan, Dictionary<string, (object Key, Guid Id)> domains, IReadOnlyList<ConfigurationRow> rows, List<object> built)
    {
        var owners = new List<ConfigurationSetOwner>(rows.Count);
        var byDomain = new Dictionary<Guid, TConfiguration>(rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            if (!rows[i].Columns.TryGetValue(plan.ForeignKey, out var domainKey) || domainKey is null)
                return GenericResult<OwnedRows>.Failure(DefaultConfigurationProviderLog.SetChildKeyUnreadable(_logger, plan.Own.Name, plan.ForeignKey));

            if (!domains.TryGetValue(ChildSetLoader.KeyText(domainKey), out var domain))
                return GenericResult<OwnedRows>.Failure(
                    DefaultConfigurationProviderLog.SetChildOrphaned(_logger, plan.Own.Name, ChildSetLoader.KeyText(domainKey)));

            if (rows[i].Number(plan.OwnPhysical) is not { } rowId || built[i] is not TConfiguration record)
                return GenericResult<OwnedRows>.Failure(DefaultConfigurationProviderLog.SetChildKeyUnreadable(_logger, plan.Own.Name, plan.OwnPhysical));

            owners.Add(new ConfigurationSetOwner(record, rowId));
            byDomain[domain.Id] = record;
        }

        return GenericResult<OwnedRows>.Success(new OwnedRows(owners, byDomain));
    }

    /// <summary>Writes the row and every child that hangs from it into the domain provider's unit of work.</summary>
    /// <remarks>
    /// The row's Foreign key to the domain container is filled from the domain row's key values, through
    /// the key the domain container's configuration lists as referencing this container. The children are
    /// written the same way under the key values this row's write returns.
    /// </remarks>
    internal async Task<IGenericResult<ConfigurationWriteResult>> Save(
        TConfiguration record,
        IDataContainer domain,
        IReadOnlyDictionary<string, object?> domainKeyValues,
        ConfigurationWriteScope unit,
        CancellationToken cancellationToken)
    {
        // Why before any row is touched: a configuration its validator refuses (or whose class declares neither a validator nor why it needs none)
        // is refused with nothing written, so there is nothing to roll back (FDW-3541).
        var validated = ConfigurationValidatorRunner.Validate(record, _logger);
        if (validated.IsFailure) return validated.ToNewResult<ConfigurationWriteResult>();

        var own = unit.Container(PathName, _commands.TableName);
        if (own.IsFailure) return own.ToNewResult<ConfigurationWriteResult>();

        var foreignKey = unit.ForeignKeyValues(domain, own.Value!.Name, domainKeyValues);
        if (foreignKey.IsFailure) return foreignKey.ToNewResult<ConfigurationWriteResult>();

        var written = await unit.Write(
            _commands.Create(DataStoreName, PathName, record, foreignKey.Value!), Target, cancellationToken).ConfigureAwait(false);
        if (written.IsFailure) return written;
        if (written.Value?.Written is not { } keys)
            return GenericResult<ConfigurationWriteResult>.Failure(
                DefaultConfigurationProviderLog.WriteReturnedNoRow(_logger, own.Value.Name));

        DefaultConfigurationProviderLog.ImplementationUnderDomain(
            _logger, typeof(TConfiguration).Name, own.Value.Name, "written", domain.Name, written.Value.Retired.Count);

        // The version this write replaced keeps its children, retired with it: they are current only
        // under the current owner row, and the new version's children are inserted fresh below.
        var replaced = await RetireCollections(typeof(TConfiguration), own.Value, written.Value.Retired, markDeleted: false, unit, cancellationToken).ConfigureAwait(false);
        if (!replaced.IsSuccess) return replaced.ToNewResult<ConfigurationWriteResult>();

        var cascade = await CascadeCollections(record, own.Value, keys, unit, cancellationToken).ConfigureAwait(false);
        return cascade.IsSuccess ? written : cascade.ToNewResult<ConfigurationWriteResult>();
    }

    /// <summary>Retires the row under its domain row and every child under it, marking them deleted, inside the domain provider's unit of work.</summary>
    internal Task<IGenericResult> Delete(
        IDataContainer domain,
        IReadOnlyDictionary<string, object?> domainKeyValues,
        ConfigurationWriteScope unit,
        CancellationToken cancellationToken)
        => Retire(domain, domainKeyValues, markDeleted: true, unit, cancellationToken);

    /// <summary>Retires the row under its domain row and every child under it, leaving them in history, inside the domain provider's unit of work.</summary>
    /// <remarks>
    /// For the domain row's new version: the rows that hang from the old version's keys stop being
    /// current, and the row written under the new version's keys replaces them.
    /// </remarks>
    internal Task<IGenericResult> Supersede(
        IDataContainer domain,
        IReadOnlyDictionary<string, object?> domainKeyValues,
        ConfigurationWriteScope unit,
        CancellationToken cancellationToken)
        => Retire(domain, domainKeyValues, markDeleted: false, unit, cancellationToken);

    private async Task<IGenericResult> Retire(
        IDataContainer domain,
        IReadOnlyDictionary<string, object?> domainKeyValues,
        bool markDeleted,
        ConfigurationWriteScope unit,
        CancellationToken cancellationToken)
    {
        var own = unit.Container(PathName, _commands.TableName);
        if (own.IsFailure) return own;

        var foreignKey = unit.ForeignKeyValues(domain, own.Value!.Name, domainKeyValues);
        if (foreignKey.IsFailure) return foreignKey;

        var retired = await unit.Write(
            _commands.Retire(DataStoreName, PathName, foreignKey.Value!, markDeleted), Target, cancellationToken).ConfigureAwait(false);
        if (retired.IsFailure) return retired;

        DefaultConfigurationProviderLog.ImplementationUnderDomain(
            _logger, typeof(TConfiguration).Name, own.Value.Name, markDeleted ? "deleted" : "superseded", domain.Name, retired.Value!.Retired.Count);

        return await RetireCollections(typeof(TConfiguration), own.Value, retired.Value.Retired, markDeleted, unit, cancellationToken).ConfigureAwait(false);
    }

    // An implementation row is always reached through its domain: the id is the DOMAIN row's, and
    // the join resolves it to this row on the domain's RowId. There is no implementation without a
    // domain, so there is no other way in and nothing to decide.
    private async Task<IGenericResult<IEnumerable<TConfiguration>>> Read(
        Guid domainId, DateTimeOffset? asOf, int levels, CancellationToken ct, ConfigurationWriteScope? unit = null)
    {
        if (domainId == Guid.Empty)
            return GenericResult<IEnumerable<TConfiguration>>.Success([]);

        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<IEnumerable<TConfiguration>>();

        var join = DomainJoin();
        if (join.IsFailure) return join.ToNewResult<IEnumerable<TConfiguration>>();
        var (foreignKey, domainContainer, joinColumn, keyColumn) = join.Value!;

        // Inside a unit, on the unit: a delete that is one of several reads rows the unit has
        // already locked, and a second connection would wait on them until the command times out.
        var command = _commands.GetByParentJoin(
            DataStoreName, PathName, foreignKey, domainContainer, joinColumn, keyColumn, domainId, asOf);
        var rows = unit is null
            ? await gateway.Value!.Execute<IEnumerable<TConfiguration>>(command, Target, ct).ConfigureAwait(false)
            : await unit.Read<IEnumerable<TConfiguration>>(command, Target, ct).ConfigureAwait(false);
        if (!rows.IsSuccess) return rows;

        // The implementation row's children hang from its RowId. It is reached the way this read reached
        // the row: through its Foreign key to the domain row, whose durable Id is the one asked for.
        var owner = new OwnerFilter(domainContainer, keyColumn, domainId, new DomainHop(domainContainer, foreignKey, joinColumn));
        var composed = await Compose(rows.Value, asOf, levels, ct, owner).ConfigureAwait(false);
        return composed.IsSuccess
            ? GenericResult<IEnumerable<TConfiguration>>.Success(composed.Value!)
            : composed.ToNewResult<IEnumerable<TConfiguration>>();
    }



    // The join from this implementation's rows to their domain rows: this container's owner key to the
    // domain provider's container (named when the domain provider registered this provider), the domain
    // field it references, and the domain's durable-Id field that reads filter on.
    private IGenericResult<(string ForeignKey, string DomainContainer, string JoinColumn, string KeyColumn)> DomainJoin()
    {
        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<(string, string, string, string)>();

        var container = gateway.Value!.DataStores
            .FirstOrDefault(s => string.Equals(s.Name, DataStoreName, StringComparison.Ordinal))?
            .Path(PathName);
        if (container is null || !container.IsSuccess || container.Value is null)
            return GenericResult<(string, string, string, string)>.Failure(
                DefaultConfigurationProviderLog.ContainerNotFoundInStore(_logger, typeof(TConfiguration).Name, _commands.TableName, DataStoreName));

        var own = container.Value.Container(_commands.TableName);
        if (!own.IsSuccess || own.Value is null)
            return GenericResult<(string, string, string, string)>.Failure(
                DefaultConfigurationProviderLog.ContainerNotFoundInStore(_logger, typeof(TConfiguration).Name, _commands.TableName, DataStoreName));

        // The key to the domain is this container's owner key to the domain provider's container,
        // which the domain provider named when it registered this provider.
        if (_domainContainer is null)
            return GenericResult<(string, string, string, string)>.Failure(
                DefaultConfigurationProviderLog.DomainNotAttached(_logger, typeof(TConfiguration).Name, _commands.TableName));

        var domain = container.Value.Container(_domainContainer);
        if (!domain.IsSuccess || domain.Value is null)
            return GenericResult<(string, string, string, string)>.Failure(
                DefaultConfigurationProviderLog.ContainerNotFoundInStore(_logger, typeof(TConfiguration).Name, _domainContainer, DataStoreName));

        var binding = ConfigurationWriteScope.OwnerBinding(_logger, domain.Value, own.Value.Name);
        if (binding.IsFailure) return binding.ToNewResult<(string, string, string, string)>();
        var fk = binding.Value!.Key;

        var joinColumn = fk.KeyFields[0].ReferencedField!.Name;              // the domain's Physical key field the FK holds
        var keyColumn = ConfigurationKeys.FieldOf(domain.Value.Keys, "Logical");         // the domain's durable Id — what filters
        if (keyColumn is null)
            return GenericResult<(string, string, string, string)>.Failure(
                DefaultConfigurationProviderLog.NoSuitableKeyForContainer(_logger, typeof(TConfiguration).Name, domain.Value.Name));

        return GenericResult<(string ForeignKey, string DomainContainer, string JoinColumn, string KeyColumn)>.Success(
            (fk.KeyFields[0].LocalField.Name, domain.Value.Name, joinColumn, keyColumn));
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
        Guid domainId, int levels, CancellationToken cancellationToken)
    {
        var found = await Get(domainId, levels, cancellationToken).ConfigureAwait(false);
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

    async Task<IGenericResult<IReadOnlyDictionary<Guid, TContract>>> IImplementationConfigurationProvider<TContract>.GetSet(
        IEnumerable<Guid> domainIds, int levels, CancellationToken cancellationToken)
    {
        var found = await GetSet(domainIds, levels, cancellationToken).ConfigureAwait(false);
        return found.IsSuccess
            ? GenericResult<IReadOnlyDictionary<Guid, TContract>>.Success(found.Value!.ToDictionary(pair => pair.Key, pair => (TContract)pair.Value))
            : found.ToNewResult<IReadOnlyDictionary<Guid, TContract>>();
    }

    async Task<IGenericResult<ConfigurationWriteResult>> IConfigurationWriteParticipant<TContract>.Save(
        TContract record,
        IDataContainer domain,
        IReadOnlyDictionary<string, object?> domainKeyValues,
        ConfigurationWriteScope unit,
        CancellationToken cancellationToken)
        => record is TConfiguration typed
            ? await Save(typed, domain, domainKeyValues, unit, cancellationToken).ConfigureAwait(false)
            : GenericResult<ConfigurationWriteResult>.Failure(
                DefaultConfigurationProviderLog.UntypedSaveTypeMismatch(
                    _logger, typeof(TConfiguration).Name, record?.GetType().Name ?? LogPlaceholders.Null));

    void IConfigurationWriteParticipant<TContract>.AttachDomain(string domainContainerName)
    {
        _domainContainer = domainContainerName;
        DefaultConfigurationProviderLog.DomainAttached(_logger, typeof(TConfiguration).Name, _commands.TableName, domainContainerName);
    }

    Task<IGenericResult> IConfigurationWriteParticipant<TContract>.Delete(
        IDataContainer domain,
        IReadOnlyDictionary<string, object?> domainKeyValues,
        ConfigurationWriteScope unit,
        CancellationToken cancellationToken) =>
        Delete(domain, domainKeyValues, unit, cancellationToken);

    Task<IGenericResult> IConfigurationSupersedeParticipant<TContract>.Supersede(
        IDataContainer domain,
        IReadOnlyDictionary<string, object?> domainKeyValues,
        ConfigurationWriteScope unit,
        CancellationToken cancellationToken) =>
        Supersede(domain, domainKeyValues, unit, cancellationToken);

    // ── the child cascade ───────────────────────────────────────────────────
    // An implementation's children hang from its row. The generated mapper lists each child collection;
    // its table is the one its element type's command declares (a name/value collection's, the one its
    // [ConfigurationChildTable] declares), and the column holding the owner is the Foreign key that table
    // declares to the owner's container, read from the DataStore configuration. A read joins on that key
    // and recurses; a write fills it from the key values the owner's write returns.

    private async Task<IGenericResult<IReadOnlyList<TConfiguration>>> Compose(
        IEnumerable<TConfiguration>? rows, DateTimeOffset? asOf, int levels, CancellationToken ct, OwnerFilter? owner = null)
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

            var children = await LoadChildrenInto(detached, mapper, _commands.TableName, asOf, levels, ct, owner).ConfigureAwait(false);
            if (children.IsFailure)
                return GenericResult<IReadOnlyList<TConfiguration>>.Failure(
                    DefaultConfigurationProviderLog.AggregateChildrenUnloaded(_logger, typeof(TConfiguration).Name, children.CurrentMessage));

            composed.Add(detached);
        }

        return GenericResult<IReadOnlyList<TConfiguration>>.Success(composed);
    }

    // Which owner row a child query is filtered to. An implementation row is reached through its Foreign
    // key to the domain row, filtered on the domain's durable Id (Hop set). A child row that owns rows of
    // its own is filtered on the key its container declares.
    private sealed record DomainHop(string DomainContainer, string OwnerForeignKey, string DomainJoinColumn);

    private sealed record OwnerFilter(string Container, string Column, Guid Id, DomainHop? Hop = null);

    // Why a result: a record whose children could not be loaded used to load as a success without
    // them, and looked complete to every caller. Every failure below now reaches Compose, which
    // refuses the record instead.
    // A level is one hop down the cascade, name/value collections included: each is a query per owner
    // row. At zero the row is composed without its collections, which stay empty -- not read, not none.
    private async Task<IGenericResult> LoadChildrenInto(object ownerRow, IPocoMapper ownerMapper, string ownerContainerName, DateTimeOffset? asOf, int levels, CancellationToken ct, OwnerFilter? owner = null)
    {
        // A list of the owner's own type is a tree, attached by the domain provider (see ConfigurationTrees); it is not a child table.
        var descriptors = ownerMapper.CascadeChildren.Where(d => !ConfigurationTrees.IsTree(d, ownerRow.GetType())).ToList();
        if (descriptors.Count == 0 || levels <= 0)
            return GenericResult.Success();

        if (owner is not null)
        {
            for (var i = 0; i < descriptors.Count; i++)
            {
                var loaded = await LoadChild(ownerRow, ownerContainerName, owner, descriptors[i], asOf, levels, ct).ConfigureAwait(false);
                if (loaded.IsFailure)
                    return loaded;
            }

            return GenericResult.Success();
        }

        var keys = LookupKeys(ownerContainerName, ownerRow.GetType().Name);
        if (keys.IsFailure)
            return keys;

        // The owner's identity is whatever its container declares as the Logical key, read by that
        // name. It used to be read from a property hardcoded to "Id", which stopped resolving when
        // an implementation record became identified by its domain ({Domain}Id) instead of carrying
        // an Id of its own: the lookup returned Guid.Empty and this method returned early, so the
        // aggregate loaded with NO children and said nothing. AuthDb came back with zero paths and
        // every token check failed on a path that was present in the database the whole time.
        var parameters = ownerMapper.MapToParameters(ownerRow);
        if (!parameters.TryGetValue(keys.Value.Logical, out var idObj) || idObj is not Guid ownerId)
            return GenericResult.Failure(DefaultConfigurationProviderLog.OwnerLogicalKeyMissing(
                _logger, ownerRow.GetType().Name, ownerContainerName, keys.Value.Logical));

        if (ownerId == Guid.Empty)
            return GenericResult.Failure(DefaultConfigurationProviderLog.OwnerLogicalKeyEmpty(
                _logger, ownerRow.GetType().Name, ownerContainerName, keys.Value.Logical));

        var byOwnKey = new OwnerFilter(ownerContainerName, keys.Value.Logical, ownerId);
        for (var i = 0; i < descriptors.Count; i++)
        {
            var loaded = await LoadChild(ownerRow, ownerContainerName, byOwnKey, descriptors[i], asOf, levels, ct).ConfigureAwait(false);
            if (loaded.IsFailure)
                return loaded;
        }

        return GenericResult.Success();
    }

    // Why the lookups' own messages: each has already logged its cause; this carries it up unchanged.
    private async Task<IGenericResult> LoadChild(object ownerRow, string ownerContainer, OwnerFilter owner, IChildCascadeDescriptor descriptor, DateTimeOffset? asOf, int levels, CancellationToken ct)
    {
        var childContainer = ChildContainer(descriptor, ownerRow.GetType().Name);
        if (childContainer.IsFailure)
            return childContainer;

        var join = ForeignKeyToOwner(ownerContainer, childContainer.Value!);
        if (join.IsFailure)
            return join;

        return descriptor.IsPropertyCollection
            ? await LoadKvpChild(ownerRow, descriptor, childContainer.Value!, ownerContainer, join.Value.OwnerColumn, owner, join.Value.ChildColumn, asOf, ct).ConfigureAwait(false)
            : await LoadTypedListChild(ownerRow, descriptor, childContainer.Value!, ownerContainer, join.Value.OwnerColumn, owner, join.Value.ChildColumn, asOf, levels, ct).ConfigureAwait(false);
    }

    private async Task<IGenericResult> LoadKvpChild(
        object ownerRow,
        IChildCascadeDescriptor descriptor,
        string childContainerName,
        string ownerContainer,
        string ownerJoinColumn,
        OwnerFilter owner,
        string fkColumn,
        DateTimeOffset? asOf,
        CancellationToken ct)
    {
        var container = LookupContainer(childContainerName);
        if (container.IsFailure)
            return container;

        var target = ContainerTarget(container.Value!);
        var cmd = BuildChildJoinQuery(DataStoreName, container.Value!, fkColumn, ownerContainer, ownerJoinColumn, owner, asOf);
        var gateway = Gateway();
        if (gateway.IsFailure)
            return gateway;

        var kvpResult = await gateway.Value!.Execute<IEnumerable<KeyValueRow>>(cmd, target, ct).ConfigureAwait(false);
        if (!kvpResult.IsSuccess)
            return kvpResult;

        if (kvpResult.Value is null)
            return GenericResult.Failure(DefaultConfigurationProviderLog.ChildQueryReturnedNoResult(
                _logger, childContainerName, owner.Id.ToString()));

        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var kvp in kvpResult.Value)
        {
            if (!string.IsNullOrEmpty(kvp.Name))
                values[kvp.Name] = kvp.Value;
        }

        descriptor.FillDictionary(ownerRow, values);
        return GenericResult.Success();
    }

    private async Task<IGenericResult> LoadTypedListChild(
        object ownerRow,
        IChildCascadeDescriptor descriptor,
        string childContainerName,
        string ownerContainer,
        string ownerJoinColumn,
        OwnerFilter owner,
        string fkColumn,
        DateTimeOffset? asOf,
        int levels,
        CancellationToken ct)
    {
        var items = await ReadChildRows(
            ownerRow.GetType().Name, descriptor, childContainerName, ownerContainer, ownerJoinColumn, owner, fkColumn, asOf, levels - 1, ct).ConfigureAwait(false);
        if (items.IsFailure)
            return items;

        descriptor.SetCollection(ownerRow, items.Value!);
        DefaultConfigurationProviderLog.ChildReadCompleted(_logger, childContainerName, owner.Id.ToString(), items.Value!.Count);
        return GenericResult.Success();
    }

    // One owner's rows of one typed-list collection, each composed to childLevels. The owner row is
    // never touched here, so the same read serves a whole-aggregate compose and a single-collection
    // read that has no owner row at all.
    private async Task<IGenericResult<IList>> ReadChildRows(
        string ownerTypeName,
        IChildCascadeDescriptor descriptor,
        string childContainerName,
        string ownerContainer,
        string ownerJoinColumn,
        OwnerFilter owner,
        string fkColumn,
        DateTimeOffset? asOf,
        int childLevels,
        CancellationToken ct)
    {
        var childMapper = PocoMapperCollection.ByName(descriptor.ChildTypeName);
        if (childMapper == PocoMapperCollection.NotFound)
            return GenericResult<IList>.Failure(DefaultConfigurationProviderLog.ChildMapperMissing(
                _logger, descriptor.ChildTypeName, ownerTypeName));

        var container = LookupContainer(childContainerName);
        if (container.IsFailure)
            return container.ToNewResult<IList>();

        var target = ContainerTarget(container.Value!);
        DefaultConfigurationProviderLog.ChildReadStarting(_logger, DataStoreName, container.Value!.Parent.Name, childContainerName, ownerContainer, owner.Id.ToString(), fkColumn);
        var cmd = BuildChildJoinQuery(DataStoreName, container.Value!, fkColumn, ownerContainer, ownerJoinColumn, owner, asOf);
        var gateway = Gateway();
        if (gateway.IsFailure)
            return gateway.ToNewResult<IList>();

        var rowsResult = await gateway.Value!.Execute(cmd, target, descriptor.ChildType, ct).ConfigureAwait(false);
        if (!rowsResult.IsSuccess)
            return rowsResult.ToNewResult<IList>();

        if (rowsResult.Value is null)
            return GenericResult<IList>.Failure(DefaultConfigurationProviderLog.ChildQueryReturnedNoResult(
                _logger, childContainerName, owner.Id.ToString()));

        // A copy of each row, never the row itself. The gateway caches this read and hands the same row objects
        // to every reader, and the grandchildren below are set ON these items: composing onto a shared row is
        // how one request saw another's half-built aggregate (API-164, FDW-742). The copy is made from the
        // row's own typed values, so it is the same record with empty collections to fill.
        var typedList = childMapper.CreateList();
        foreach (var item in rowsResult.Value)
        {
            var copy = childMapper.MapFromDictionary(childMapper.MapToParameters(item).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal));
            if (copy.IsFailure || copy.Value is not { } detached)
                return copy.ToNewResult<IList>();

            typedList.Add(detached);
        }

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
            if (item is null)
                continue;

            var grandchildren = await LoadChildrenInto(item, childMapper, childContainerName, asOf, childLevels, ct).ConfigureAwait(false);
            if (grandchildren.IsFailure)
                return grandchildren.ToNewResult<IList>();
        }

        return GenericResult<IList>.Success(typedList);
    }

    /// <summary>Builds the query that loads one child collection of an implementation row.</summary>
    /// <remarks>
    /// Filters the child's own IsCurrent and IsDeleted as well as the owner's. Filtering only the
    /// owner composed every retired version and every deleted row of the child into the collection:
    /// a detached workspace resource stayed on the map, and readiness kept counting it (FDW-795).
    /// </remarks>
    private static IDataCommand BuildChildJoinQuery(
        string dataStoreName,
        IDataContainer child,
        string fkColumn,
        string ownerContainer,
        string ownerJoinColumn,
        OwnerFilter owner,
        DateTimeOffset? asOf)
    {
        var builder = new QueryCommandBuilder<object>(dataStoreName, child.Parent.Name, child.Name)
            .Join(ownerContainer, fkColumn, ownerJoinColumn);

        // An implementation owner: on from its row to the domain row, through its Foreign key to it.
        if (owner.Hop is { } hop)
            builder = builder
                .JoinFrom(ownerContainer, hop.DomainContainer, hop.OwnerForeignKey, hop.DomainJoinColumn)
                .Where(string.Concat(hop.DomainContainer, ".IsCurrent"), true)
                .Where(string.Concat(hop.DomainContainer, ".IsDeleted"), false);

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
            builder = builder.Where("IsCurrent", true);

        return builder
            .Where("IsDeleted", false)
            .Where(string.Concat(ownerContainer, ".IsDeleted"), false)
            .Where(string.Concat(owner.Container, ".", owner.Column), owner.Id)
            .Build().Command;
    }


    private IGenericResult<(string Physical, string Logical)> LookupKeys(string containerName, string rowTypeName)
    {
        var containerResult = LookupContainer(containerName);
        if (containerResult.IsFailure)
            return containerResult.ToNewResult<(string Physical, string Logical)>();

        var physical = ConfigurationKeys.FieldOf(containerResult.Value!.Keys, "Physical");
        if (physical is null)
            return GenericResult<(string Physical, string Logical)>.Failure(
                DefaultConfigurationProviderLog.ContainerKeyNotDeclared(_logger, containerName, "Physical"));

        var logical = ConfigurationKeys.FieldOf(containerResult.Value.Keys, "Logical");
        if (logical is null)
            return GenericResult<(string Physical, string Logical)>.Failure(
                DefaultConfigurationProviderLog.ContainerKeyNotDeclared(_logger, containerName, "Logical"));

        return GenericResult<(string Physical, string Logical)>.Success((physical, logical));
    }

    // The table a child collection's rows live in: a typed-list child's is the one its element type's
    // ConfigurationCommands TypeOption declares; a name/value child's is the one its
    // [ConfigurationChildTable] declares. Neither is derived from a name.
    private IGenericResult<string> ChildContainer(IChildCascadeDescriptor descriptor, string ownerTypeName)
    {
        if (descriptor.IsPropertyCollection)
            return string.IsNullOrEmpty(descriptor.ChildContainerName)
                ? GenericResult<string>.Failure(
                    DefaultConfigurationProviderLog.ChildTableNotDeclared(_logger, ownerTypeName, descriptor.BoundPropertyName))
                : GenericResult<string>.Success(descriptor.ChildContainerName);

        return ChildCommand(descriptor.ChildType) is { } command
            ? GenericResult<string>.Success(command.ContainerName)
            : GenericResult<string>.Failure(
                DefaultConfigurationProviderLog.NoChildCommandForType(_logger, ownerTypeName, descriptor.ChildTypeName));
    }

    private static IConfigurationCommands? ChildCommand(Type childType)
        => ConfigurationCommands.All().FirstOrDefault(c => c.ConfigType == childType);

    // The Foreign key the child's container declares to the owner's container, from the owner's configured
    // ReferencingKeys: its column on the child, and the owner field it references.
    private IGenericResult<(string ChildColumn, string OwnerColumn)> ForeignKeyToOwner(string ownerContainerName, string childContainerName)
    {
        var owner = LookupContainer(ownerContainerName);
        if (owner.IsFailure) return owner.ToNewResult<(string, string)>();

        var binding = ConfigurationWriteScope.OwnerBinding(_logger, owner.Value!, childContainerName);
        if (binding.IsFailure) return binding.ToNewResult<(string, string)>();

        var key = binding.Value!.Key;
        if (key.KeyFields.Count != 1)
            return GenericResult<(string, string)>.Failure(
                DefaultConfigurationProviderLog.CompositeForeignKeyToParent(_logger, key.KeyName, childContainerName, ownerContainerName, key.KeyFields.Count));
        if (key.KeyFields[0].ReferencedField is not { } referenced)
            return GenericResult<(string, string)>.Failure(
                DefaultConfigurationProviderLog.ReferencedValueMissing(_logger, key.KeyName, childContainerName, ownerContainerName, "(unresolved)"));

        DefaultConfigurationProviderLog.ForeignKeyFilled(
            _logger, key.KeyName, childContainerName, ownerContainerName, $"{key.KeyFields[0].LocalField.Name}={referenced.Name}");
        return GenericResult<(string, string)>.Success((key.KeyFields[0].LocalField.Name, referenced.Name));
    }

    // A container is found wherever the data store declares it: a child need not be in this provider's
    // path (transform.FieldMappingTransformation hangs from data.DataSetFieldMapping). Container names
    // are unique within a store; none, or more than one, is a failure.
    private IGenericResult<IDataContainer> LookupContainer(string containerName)
    {
        var gateway = Gateway();
        if (gateway.IsFailure) return gateway.ToNewResult<IDataContainer>();

        var store = gateway.Value!.DataStores
            .FirstOrDefault(st => string.Equals(st.Name, DataStoreName, StringComparison.Ordinal));
        if (store is null)
            return GenericResult<IDataContainer>.Failure(
                DefaultConfigurationProviderLog.DataStoreNotFound(_logger, containerName, DataStoreName));

        var matches = store.Paths
            .SelectMany(p => p.Containers)
            .Where(c => string.Equals(c.Name, containerName, StringComparison.Ordinal))
            .ToList();
        if (matches.Count == 0)
            return GenericResult<IDataContainer>.Failure(
                DefaultConfigurationProviderLog.ContainerNotFoundInStore(_logger, typeof(TConfiguration).Name, containerName, DataStoreName));
        if (matches.Count > 1)
            return GenericResult<IDataContainer>.Failure(
                DefaultConfigurationProviderLog.ContainerDeclaredMoreThanOnce(_logger, containerName, DataStoreName, matches.Count));

        return GenericResult<IDataContainer>.Success(matches[0]);
    }

    // Reads and writes of a container target the path the store declares it in.
    private DataStoreTarget ContainerTarget(IDataContainer container) => new(DataStoreName, container.Parent.Name, container.Name);

    // Writes every child the record carries under the row whose key values the owner's write returned.
    private async Task<IGenericResult> CascadeCollections(
        IGenericConfiguration owner,
        IDataContainer ownerContainer,
        IReadOnlyDictionary<string, object?> ownerKeyValues,
        ConfigurationWriteScope unit,
        CancellationToken ct)
    {
        // The read of this type fails without a mapper, so a write that went on without one would
        // report success while writing no children and retiring none of the old ones.
        var mapper = PocoMapperCollection.ByName(owner.GetType().Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.CascadeMapperMissing(_logger, owner.GetType().Name, "save"));

        // A tree list holds members of their own, saved on their own; saving the parent does not rewrite them.
        var descriptors = mapper.CascadeChildren.Where(d => !ConfigurationTrees.IsTree(d, owner.GetType())).ToList();
        for (var c = 0; c < descriptors.Count; c++)
        {
            var treatment = TreatmentOf(owner, descriptors[c], ownerContainer);
            if (treatment.IsFailure) return treatment;

            var result = descriptors[c].IsPropertyCollection
                ? await SaveKvpChild(owner, descriptors[c], ownerContainer, ownerKeyValues, treatment.Value, unit, ct).ConfigureAwait(false)
                : await SaveListChildren(owner, descriptors[c], ownerContainer, ownerKeyValues, treatment.Value, unit, ct).ConfigureAwait(false);
            if (!result.IsSuccess) return result;
        }

        return GenericResult.Success();
    }

    // How a save treats one child collection. A collection whose container hangs from the owner's row is retired with the replaced version and
    // written again, and has no mode. A collection whose container hangs from the owner's durable Id outlives the owner's versions, so it
    // declares how a save treats it (FDW-4240): there is no default, and one that declares none, or one the modes do not know, is refused.
    private IGenericResult<IChildCascadeMode?> TreatmentOf(IGenericConfiguration owner, IChildCascadeDescriptor descriptor, IDataContainer ownerContainer)
    {
        var ownerType = owner.GetType();
        var holds = HoldsDurableId(ownerType, descriptor, ownerContainer, NothingToSave(owner, descriptor));
        if (holds.IsFailure) return holds.ToNewResult<IChildCascadeMode?>();
        if (!holds.Value.Durable) return GenericResult<IChildCascadeMode?>.Success(null);

        if (string.IsNullOrEmpty(descriptor.CascadeModeName))
            return GenericResult<IChildCascadeMode?>.Failure(
                DefaultConfigurationProviderLog.ChildCascadeModeNotDeclared(_logger, ownerType.Name, descriptor.BoundPropertyName, holds.Value.Container));

        var mode = ChildCascadeModes.ByName(descriptor.CascadeModeName);
        return ReferenceEquals(mode, ChildCascadeModes.NotFound)
            ? GenericResult<IChildCascadeMode?>.Failure(
                DefaultConfigurationProviderLog.ChildCascadeModeUnknown(_logger, ownerType.Name, descriptor.BoundPropertyName, descriptor.CascadeModeName))
            : GenericResult<IChildCascadeMode?>.Success(mode);
    }

    // Whether a collection's container hangs from the owner's durable Id, and the container it is written to. A container that declares no link to
    // its owner can hold no children, so an empty collection of it has nothing to write or retire and is not an error; a collection with rows is.
    private IGenericResult<(bool Durable, string Container)> HoldsDurableId(
        Type ownerType, IChildCascadeDescriptor descriptor, IDataContainer ownerContainer, bool nothingToSave)
    {
        var childName = ChildContainer(descriptor, ownerType.Name);
        if (childName.IsFailure) return childName.ToNewResult<(bool, string)>();

        var binding = ConfigurationWriteScope.OwnerBinding(_logger, ownerContainer, childName.Value!);
        if (binding.IsFailure)
            return nothingToSave
                ? GenericResult<(bool, string)>.Success((false, childName.Value!))
                : binding.ToNewResult<(bool, string)>();

        return GenericResult<(bool, string)>.Success((ConfigurationWriteScope.HoldsDurableId(ownerContainer, binding.Value!.Key), childName.Value!));
    }

    // Whether the owner carries no row of a collection: unset, or empty.
    private static bool NothingToSave(IGenericConfiguration owner, IChildCascadeDescriptor descriptor)
        => descriptor.IsPropertyCollection
            ? descriptor.ReadDictionary(owner) is not { Count: > 0 }
            : descriptor.GetCollection(owner) is not IEnumerable items || !items.Cast<object>().Any();

    private async Task<IGenericResult> SaveListChildren(
        IGenericConfiguration owner,
        IChildCascadeDescriptor descriptor,
        IDataContainer ownerContainer,
        IReadOnlyDictionary<string, object?> ownerKeyValues,
        IChildCascadeMode? mode,
        ConfigurationWriteScope unit,
        CancellationToken ct)
    {
        if (descriptor.GetCollection(owner) is not System.Collections.IEnumerable items)
            return GenericResult.Success();

        var kept = new List<Guid>();
        foreach (var item in items)
        {
            // Why this is logged rather than skipped quietly: a type-test `continue` treats
            // "did not match" as "nothing to do", so the row is dropped and NOTHING reports
            // it — not the build, not the save result, not an audit trail. That silence is
            // why the Workspace children were discarded unnoticed. A rewrite at least leaves
            // evidence; a silent skip leaves none, so it has to announce itself.
            if (item is not IGenericConfiguration childCfg)
            {
                DefaultConfigurationProviderLog.ChildSkippedNotConfiguration(
                    _logger, owner.GetType().Name, item?.GetType().Name ?? LogPlaceholders.NullName);
                continue;
            }

            var itemResult = await SaveOneChild(childCfg, ownerContainer, ownerKeyValues, mode is not null, unit, ct).ConfigureAwait(false);
            if (!itemResult.IsSuccess) return itemResult;
            kept.Add(childCfg.Id);
        }

        // Only a collection that declares its list the full set retires the children the list no longer carries.
        return mode is { RetiresAbsentChildren: true }
            ? await RetireAbsent(owner, descriptor, ownerContainer, ownerKeyValues, kept, unit, ct).ConfigureAwait(false)
            : GenericResult.Success();
    }

    // Retires, under the owner's durable Id, the current children the saved list does not carry, then their own children.
    private async Task<IGenericResult> RetireAbsent(
        IGenericConfiguration owner,
        IChildCascadeDescriptor descriptor,
        IDataContainer ownerContainer,
        IReadOnlyDictionary<string, object?> ownerKeyValues,
        IReadOnlyList<Guid> kept,
        ConfigurationWriteScope unit,
        CancellationToken ct)
    {
        var childName = ChildContainer(descriptor, owner.GetType().Name);
        if (childName.IsFailure) return childName;

        var child = LookupContainer(childName.Value!);
        if (child.IsFailure) return child;

        var logical = ConfigurationKeys.FieldOf(child.Value!.Keys, "Logical");
        if (logical is null)
            return GenericResult.Failure(DefaultConfigurationProviderLog.ChildLogicalKeyNotDeclared(_logger, childName.Value!));

        var foreignKey = unit.ForeignKeyValues(ownerContainer, childName.Value!, ownerKeyValues);
        if (foreignKey.IsFailure) return foreignKey;

        var current = await unit.Read<IEnumerable<Dictionary<string, object?>>>(
            CurrentChildren(child.Value!, foreignKey.Value!), ContainerTarget(child.Value!), ct).ConfigureAwait(false);
        if (!current.IsSuccess) return current;

        var absent = (current.Value ?? []).Select(row => row.TryGetValue(logical, out var id) ? id : null).OfType<Guid>().Where(id => !kept.Contains(id)).ToList();
        foreach (var childId in absent)
        {
            var retired = await RetireAbsentOne(descriptor, child.Value!, logical, childId, foreignKey.Value!, unit, ct).ConfigureAwait(false);
            if (!retired.IsSuccess) return retired;
        }

        DefaultConfigurationProviderLog.AbsentChildrenRetired(
            _logger, childName.Value!, ConfigurationWriteScope.Describe(foreignKey.Value!), absent.Count);
        return GenericResult.Success();
    }

    // Retires one child by its own Id under the owner's Id, then the children that hang from it.
    private async Task<IGenericResult> RetireAbsentOne(
        IChildCascadeDescriptor descriptor,
        IDataContainer child,
        string logical,
        Guid childId,
        IReadOnlyDictionary<string, object?> foreignKey,
        ConfigurationWriteScope unit,
        CancellationToken ct)
    {
        var match = new Dictionary<string, object?>(foreignKey, StringComparer.Ordinal) { [logical] = childId };
        var result = await unit.Write(new ConfigurationRetireCommand(match, markDeleted: false), ContainerTarget(child), ct).ConfigureAwait(false);
        return result.IsFailure
            ? result
            : await RetireCollections(descriptor.ChildType, child, result.Value!.Retired, markDeleted: false, unit, ct).ConfigureAwait(false);
    }

    // The current, undeleted rows of a child container that hang from one owner.
    private IDataCommand CurrentChildren(IDataContainer child, IReadOnlyDictionary<string, object?> foreignKey)
    {
        var query = new QueryCommandBuilder<Dictionary<string, object?>>(DataStoreName, child.Parent.Name, child.Name);
        foreach (var field in foreignKey)
            query = query.Where(field.Key, field.Value);

        return query.Where("IsCurrent", true).Where("IsDeleted", false).Build().Command;
    }

    // Saves a single composed child via its per-type ConfigurationCommand, looked up by config-TYPE
    // identity with no reflection, with the Foreign key its container declares to the owner's container
    // filled from the owner's key values; then its own children under the key values its write returns.
    private async Task<IGenericResult> SaveOneChild(
        IGenericConfiguration childCfg,
        IDataContainer ownerContainer,
        IReadOnlyDictionary<string, object?> ownerKeyValues,
        bool replacesCurrent,
        ConfigurationWriteScope unit,
        CancellationToken ct)
    {
        var validated = ConfigurationValidatorRunner.Validate(childCfg, _logger);
        if (validated.IsFailure) return validated;

        var command = ChildCommand(childCfg.GetType());
        if (command is null)
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.NoChildCommandForType(_logger, typeof(TConfiguration).Name, childCfg.GetType().Name));

        var childContainer = LookupContainer(command.ContainerName);
        if (childContainer.IsFailure) return childContainer;

        var foreignKey = unit.ForeignKeyValues(ownerContainer, command.ContainerName, ownerKeyValues);
        if (foreignKey.IsFailure) return foreignKey;

        if (childCfg.Id == Guid.Empty)
            childCfg.Id = Guid.CreateVersion7();

        // A child under the owner's durable Id replaces its own current version (matched on its Id), because the owner's versions do not
        // retire it; a child under the owner's row replaces nothing, because the replaced version's children were retired with it.
        var write = replacesCurrent
            ? command.Create(DataStoreName, childContainer.Value!.Parent.Name, childCfg, foreignKey.Value!)
            : command.Insert(DataStoreName, childContainer.Value!.Parent.Name, childCfg, foreignKey.Value!);
        var written = await unit.Write(write, ContainerTarget(childContainer.Value!), ct).ConfigureAwait(false);
        if (written.IsFailure) return written;
        if (written.Value?.Written is not { } keys)
            return GenericResult.Failure(DefaultConfigurationProviderLog.WriteReturnedNoRow(_logger, command.ContainerName));

        DefaultConfigurationProviderLog.ChildWritten(
            _logger, childCfg.GetType().Name, command.ContainerName, ownerContainer.Name, ConfigurationWriteScope.Describe(foreignKey.Value!));
        return await CascadeCollections(childCfg, childContainer.Value!, keys, unit, ct).ConfigureAwait(false);
    }

    private async Task<IGenericResult> SaveKvpChild(
        IGenericConfiguration owner,
        IChildCascadeDescriptor descriptor,
        IDataContainer ownerContainer,
        IReadOnlyDictionary<string, object?> ownerKeyValues,
        IChildCascadeMode? mode,
        ConfigurationWriteScope unit,
        CancellationToken ct)
    {
        var bag = descriptor.ReadDictionary(owner);
        if (bag is null || bag.Count == 0)
            return GenericResult.Success();

        var childContainer = ChildContainer(descriptor, owner.GetType().Name);
        if (childContainer.IsFailure) return childContainer;

        var foreignKey = unit.ForeignKeyValues(ownerContainer, childContainer.Value!, ownerKeyValues);
        if (foreignKey.IsFailure) return foreignKey;

        var container = LookupContainer(childContainer.Value!);
        if (container.IsFailure) return container;

        var target = ContainerTarget(container.Value!);
        if (mode is { RetiresAbsentChildren: true })
        {
            // The bag is the full set and its rows are not versioned one by one: those the owner's Id holds are closed and the bag written again.
            var closed = await unit.Write(new ConfigurationRetireCommand(foreignKey.Value!, markDeleted: false), target, ct).ConfigureAwait(false);
            if (closed.IsFailure) return closed;
        }

        foreach (var entry in bag)
        {
            var result = await unit.Write(
                new ConfigurationSaveCommand<KeyValueRow>(new KeyValueRow { Name = entry.Key, Value = entry.Value }, foreignKey.Value!, replacesCurrent: false),
                target, ct).ConfigureAwait(false);
            if (!result.IsSuccess) return result;
        }

        DefaultConfigurationProviderLog.KvpChildSaved(
            _logger, owner.GetType().Name, childContainer.Value!, bag.Count);
        return GenericResult.Success();
    }

    // Retires, under each retired owner row, the rows of every child container the owner's type carries,
    // marking them deleted, then those rows' own children in turn.
    private async Task<IGenericResult> RetireCollections(
        Type ownerType,
        IDataContainer ownerContainer,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> retiredOwners,
        bool markDeleted,
        ConfigurationWriteScope unit,
        CancellationToken ct)
    {
        if (retiredOwners.Count == 0)
            return GenericResult.Success();

        var mapper = PocoMapperCollection.ByName(ownerType.Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.CascadeMapperMissing(_logger, ownerType.Name, "retire"));

        // A tree list holds members of their own; retiring the parent retires its own rows, not theirs.
        var descriptors = mapper.CascadeChildren.Where(d => !ConfigurationTrees.IsTree(d, ownerType)).ToList();
        for (var c = 0; c < descriptors.Count; c++)
        {
            var childContainer = ChildContainer(descriptors[c], ownerType.Name);
            if (childContainer.IsFailure) return childContainer;

            var retired = await RetireUnder(descriptors[c], childContainer.Value!, ownerContainer, retiredOwners, markDeleted, unit, ct).ConfigureAwait(false);
            if (!retired.IsSuccess) return retired;
        }

        return GenericResult.Success();
    }

    private async Task<IGenericResult> RetireUnder(
        IChildCascadeDescriptor descriptor,
        string childContainerName,
        IDataContainer ownerContainer,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> retiredOwners,
        bool markDeleted,
        ConfigurationWriteScope unit,
        CancellationToken ct)
    {
        var childContainer = LookupContainer(childContainerName);
        if (childContainer.IsFailure) return childContainer;

        // Rows that hang from the owner's durable Id are not the replaced version's: a replace leaves them current, and the save that
        // follows treats them by the mode their collection declares. Only a delete of the owner retires them (FDW-4240).
        var binding = ConfigurationWriteScope.OwnerBinding(_logger, ownerContainer, childContainerName);
        if (binding.IsFailure) return binding;
        if (!markDeleted && ConfigurationWriteScope.HoldsDurableId(ownerContainer, binding.Value!.Key))
        {
            DefaultConfigurationProviderLog.ChildrenKeptUnderDurableId(_logger, childContainerName, ownerContainer.Name);
            return GenericResult.Success();
        }

        foreach (var owner in retiredOwners)
        {
            var foreignKey = unit.ForeignKeyValues(ownerContainer, childContainerName, owner);
            if (foreignKey.IsFailure) return foreignKey;

            var retired = await unit.Write(
                new ConfigurationRetireCommand(foreignKey.Value!, markDeleted),
                ContainerTarget(childContainer.Value!), ct).ConfigureAwait(false);
            if (retired.IsFailure) return retired;

            DefaultConfigurationProviderLog.ChildrenRetired(
                _logger, childContainerName, ownerContainer.Name, ConfigurationWriteScope.Describe(foreignKey.Value!), retired.Value!.Retired.Count, markDeleted);

            if (descriptor.IsPropertyCollection) continue;
            var nested = await RetireCollections(descriptor.ChildType, childContainer.Value!, retired.Value.Retired, markDeleted, unit, ct).ConfigureAwait(false);
            if (!nested.IsSuccess) return nested;
        }

        return GenericResult.Success();
    }

    private IGenericResult<IConfigurationGateway> Gateway() => _gatewayProvider.Get(DataStoreName);

    private DataStoreTarget Target => new(DataStoreName, PathName, _commands.TableName);
}
