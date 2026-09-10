using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Results;
using Fdw.Services.Configuration.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Configuration;

/// <summary>
/// Single-source configuration provider over the configured <see cref="IConfigurationGateway"/>
/// (ConfigurationDb). Commands come from the <c>ConfigurationCommands</c> TypeCollection.
/// </summary>
/// <typeparam name="TImplementationConfiguration">The domain's implementation contract -- the marker only this domain's implementations carry, and what a read hands back.</typeparam>
public abstract class ImplementationConfigurationProviderBase<TImplementationConfiguration>
    : IImplementationConfigurationProvider,
      IImplementationConfigurationProvider<TImplementationConfiguration>
    where TImplementationConfiguration : IImplementationConfiguration
{
    private readonly IConfigurationGatewayProvider _gatewayProvider;
    private readonly ILogger _logger;
    private readonly AsyncLocal<bool> _isQuerying = new();

    /// <summary>
    /// DataStore name this provider targets (e.g. "PlatformConfiguration"). Set at construction (the
    /// derived provider's own constructor default) or via <see cref="SetConfiguration"/> — never
    /// publicly settable directly, since nothing outside the provider has a legitimate reason to
    /// change where it reads from.
    /// </summary>
    public string DataStoreName { get; private set; }

    /// <summary>Schema/path name this provider targets (e.g. "conn", "sec"). See <see cref="DataStoreName"/>.</summary>
    public string PathName { get; private set; }

    /// <summary>
    /// Overrides this provider's target data store/path — the RARE non-default-location case (the
    /// common case is the derived provider's own constructor default, applied automatically by
    /// <c>RegisterDomainConfiguration(services)</c>). Call once, directly on the already-registered
    /// singleton instance, immediately after it is first resolved — never by registering a second
    /// time with different constructor arguments (that split-registration-mechanism pattern is the
    /// exact defect class the single idempotent registration cascade exists to prevent).
    /// </summary>
    public void SetConfiguration(string dataStoreName, string pathName)
    {
        DataStoreName = dataStoreName ?? throw new ArgumentNullException(nameof(dataStoreName));
        PathName = pathName ?? throw new ArgumentNullException(nameof(pathName));
    }

    /// <summary>Initializes the provider.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto <paramref name="dataStoreName"/>.</param>
    /// <param name="dataStoreName">The configuration connection this domain's rows live on.</param>
    /// <param name="pathName">Schema/path name (e.g. "conn", "sec").</param>
    /// <param name="tableName">The container this provider reads and writes.</param>
    protected ImplementationConfigurationProviderBase(
        ILogger<ImplementationConfigurationProviderBase<TImplementationConfiguration>>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string tableName)
    {
        _commands = new DomainConfigurationCommand(tableName);
        _logger = logger ?? NullLogger<ImplementationConfigurationProviderBase<TImplementationConfiguration>>.Instance;
        _gatewayProvider = gatewayProvider ?? throw new ArgumentNullException(nameof(gatewayProvider));
        DataStoreName = dataStoreName ?? throw new ArgumentNullException(nameof(dataStoreName));
        PathName = pathName ?? throw new ArgumentNullException(nameof(pathName));
    }






    // ── Type-erased surface ─────────────────────────────────────────────────

    // Explicit only where the erased contract and the typed one share a signature but not a return
    // type. Everything else on the two interfaces is satisfied by one public method.
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
        var found = await Get(name, ct).ConfigureAwait(false);
        return found.IsSuccess
            ? GenericResult<IImplementationConfiguration>.Success(found.Value!)
            : found.ToNewResult<IImplementationConfiguration>();
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<TImplementationConfiguration>>> Get(
        IEnumerable<Guid> domainIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainIds);

        var found = new List<TImplementationConfiguration>();
        foreach (var domainId in domainIds)
        {
            var one = await Get(domainId, cancellationToken).ConfigureAwait(false);
            if (!one.IsSuccess) return one.ToNewResult<IReadOnlyList<TImplementationConfiguration>>();
            if (one.Value is not null) found.Add(one.Value);
        }

        return GenericResult<IReadOnlyList<TImplementationConfiguration>>.Success(found);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<TImplementationConfiguration>>> Find(
        Func<TImplementationConfiguration, bool> predicate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var all = await ((IImplementationConfigurationProvider<TImplementationConfiguration>)this)
            .Get(cancellationToken).ConfigureAwait(false);
        if (!all.IsSuccess) return all;

        var matched = new List<TImplementationConfiguration>();
        foreach (var one in all.Value ?? [])
        {
            if (predicate(one)) matched.Add(one);
        }

        return GenericResult<IReadOnlyList<TImplementationConfiguration>>.Success(matched);
    }

    async Task<IGenericResult> IImplementationConfigurationProvider.Save(
        IImplementationConfiguration implementationConfiguration,
        string domain, string implementationName, string name, CancellationToken ct)
        => implementationConfiguration is TImplementationConfiguration typed
            ? await Save(typed, domain, implementationName, name, ct).ConfigureAwait(false)
            : GenericResult.Failure(
                ServicesResultCodes.ByName("InvalidConfigurationType"),
                ResultDetails.Create(
                    "ExpectedType", typeof(TImplementationConfiguration).Name,
                    "ActualType", implementationConfiguration?.GetType().Name ?? "(null)"));


    // Constructed, not resolved. A TypeOption is a singleton and could carry one table name, but
    // every domain reads a differently named table of identical shape.
    private readonly DomainConfigurationCommand _commands;

    /// <summary>Returns the command over this provider's table.</summary>
    protected DomainConfigurationCommand Commands() => _commands;

    private DataStoreTarget Target => new(DataStoreName, PathName, Commands().TableName);

    // The registry of implementation providers, keyed by the value a domain row carries in its
    // Implementation column. A provider that is not a domain never registers one, so it stays empty
    // and the dispatch below simply does not run -- no flag, no branch on what kind of provider this is.
    private readonly ConcurrentDictionary<string, IImplementationConfigurationProvider<TImplementationConfiguration>> _implementations
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Registers the provider that supplies one implementation's own configuration.</summary>
    /// <typeparam name="T">The implementation provider being registered.</typeparam>
    /// <param name="name">The value a domain row carries to name this implementation.</param>
    /// <param name="implementationConfigurationProvider">The provider to dispatch to.</param>
    /// <returns>Success, or a failure naming why the provider could not be registered.</returns>
    public IGenericResult Register<T>(string name, T implementationConfigurationProvider)
    {
        if (implementationConfigurationProvider is null)
        {
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.ProviderNotErasable(_logger, name, "(null)"));
        }

        if (implementationConfigurationProvider is not IImplementationConfigurationProvider<TImplementationConfiguration> typed)
        {
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.ProviderNotErasable(
                    _logger, name, implementationConfigurationProvider.GetType().FullName ?? "(null)"));
        }

        _implementations[name] = typed;
        DefaultConfigurationProviderLog.TypedProviderRegistered(_logger, typeof(DomainConfiguration).Name, name);
        return GenericResult.Success();
    }

    /// <inheritdoc/>
    public virtual async Task<IGenericResult<TImplementationConfiguration>> Get(string name, CancellationToken ct = default)
    {
        var domain = await GetByName(name, null, ct).ConfigureAwait(false);
        if (!domain.IsSuccess) return domain.ToNewResult<TImplementationConfiguration>();

        return await Implementation(domain.Value, null, ct).ConfigureAwait(false);
    }

    /// <summary>Writes a configured member: its domain row if there is not one, then its implementation.</summary>
    /// <typeparam name="T">The implementation configuration being written.</typeparam>
    /// <param name="implementationConfiguration">The configuration to write.</param>
    /// <param name="domain">The domain this member belongs to.</param>
    /// <param name="implementationName">Which implementation this is; it selects the provider that writes it.</param>
    /// <param name="name">The member's name, which the domain row carries.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Success, or a structured failure.</returns>
    /// <remarks>
    /// One write, not a create and an update. The name is what a member is known by, so a domain row
    /// already carrying it is the row this implementation hangs from; only when there is none is one
    /// minted. The two rows are never written apart -- a domain row naming an implementation that was
    /// not written is the record that fails to compose on the next read.
    /// </remarks>
    public async Task<IGenericResult> Save<T>(
        T implementationConfiguration, string domain, string implementationName, string name,
        CancellationToken ct = default)
        where T : TImplementationConfiguration
    {
        if (!_implementations.ContainsKey(implementationName))
        {
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.NoImplementationProvider(_logger, name, implementationName));
        }

        var existing = await GetByName(name, null, ct).ConfigureAwait(false);
        if (!existing.IsSuccess) return existing.ToNewResult<DomainConfiguration>();

        var domainId = existing.Value?.Id ?? Guid.Empty;
        if (domainId == Guid.Empty)
        {
            var record = new DomainConfiguration
            {
                Id = Guid.CreateVersion7(),
                Name = name,
                Domain = domain,
                Implementation = implementationName,
            };

            var gateway = Gateway();
            if (gateway.IsFailure) return gateway.ToNewResult<DomainConfiguration>();

            var written = await gateway.Value!.Execute<DomainConfiguration>(
                Commands().Create(DataStoreName, PathName, record), Target, ct).ConfigureAwait(false);
            if (!written.IsSuccess) return written.ToNewResult<DomainConfiguration>();

            domainId = record.Id;
        }

        return await Write(implementationConfiguration, implementationName, name, domainId, ct)
            .ConfigureAwait(false);
    }

    // The one write both overloads reach: pick the provider the implementation names, stamp the
    // domain's name and id onto the record, and hand it over.
    private async Task<IGenericResult> Write<T>(
        T implementationConfiguration, string implementationName, string name, Guid domainId, CancellationToken ct)
        where T : TImplementationConfiguration
    {
        if (!_implementations.TryGetValue(implementationName, out var provider))
        {
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.NoImplementationProvider(_logger, name, implementationName));
        }

        implementationConfiguration.Name = name;
        implementationConfiguration.Id = domainId;
        return await provider.Save(implementationConfiguration, ct).ConfigureAwait(false);
    }

    // The implementation surface. A provider registered against a domain is asked for its row by
    // that domain's Id; the join to the domain's RowId happens inside the query.
    async Task<IGenericResult<TImplementationConfiguration>> IImplementationConfigurationProvider<TImplementationConfiguration>.Get(
        Guid domainId, CancellationToken ct)
        => await Get(domainId, ct).ConfigureAwait(false);

    async Task<IGenericResult<TImplementationConfiguration>> IImplementationConfigurationProvider<TImplementationConfiguration>.Get(
        Guid domainId, DateTimeOffset asOf, CancellationToken ct)
        => await Get(domainId, asOf, ct).ConfigureAwait(false);

    async Task<IGenericResult<IReadOnlyList<TImplementationConfiguration>>> IImplementationConfigurationProvider<TImplementationConfiguration>.Get(
        CancellationToken ct)
    {
        var rows = await Get(ct).ConfigureAwait(false);
        if (!rows.IsSuccess) return rows.ToNewResult<IReadOnlyList<TImplementationConfiguration>>();

        // The rows this provider read ARE the implementations. Reading an implementation off them
        // would be treating them as domain records, which is the other half of the pair.
        var own = new List<TImplementationConfiguration>();
        foreach (var row in rows.Value ?? [])
        {
            if (row is TImplementationConfiguration mine) own.Add(mine);
        }

        return GenericResult<IReadOnlyList<TImplementationConfiguration>>.Success(own);
    }

    async Task<IGenericResult<TImplementationConfiguration>> IImplementationConfigurationProvider<TImplementationConfiguration>.Save(
        TImplementationConfiguration record, CancellationToken ct)
    {
        if (record is not DomainConfiguration typed)
        {
            return GenericResult<TImplementationConfiguration>.Failure(
                ServicesResultCodes.ByName("InvalidConfigurationType"),
                ResultDetails.Create("ExpectedType", typeof(DomainConfiguration).Name,
                                     "ActualType", record?.GetType().Name ?? "(null)"));
        }

        var saved = await WriteRow(typed, ct).ConfigureAwait(false);
        return saved.IsSuccess
            ? GenericResult<TImplementationConfiguration>.Success(record)
            : saved.ToNewResult<TImplementationConfiguration>();
    }

    /// <summary>
    /// Reads the version of a configuration that was in force at <paramref name="asOf"/>, composed
    /// with the typed body and children that belonged to that same version.
    /// </summary>
    /// <param name="name">The configuration's name.</param>
    /// <param name="asOf">The instant to read the configuration as of.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The aggregate in force at that instant, or a failure.</returns>
    /// <remarks>
    /// Only meaningful for configurations declared <c>Temporal</c>: a non-temporal table has no
    /// EffectiveStart/EffectiveEnd for the predicate to read, so the query fails rather than
    /// silently handing back the current row — a restatement that quietly used today's definition
    /// is precisely the failure this path exists to prevent.
    /// </remarks>
    public virtual async Task<IGenericResult<TImplementationConfiguration>> Get(
        string name, DateTimeOffset asOf, CancellationToken ct = default)
    {
        var domain = await GetByName(name, asOf, ct).ConfigureAwait(false);
        if (!domain.IsSuccess) return domain.ToNewResult<TImplementationConfiguration>();

        return await Implementation(domain.Value, asOf, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Composes a domain row into its full aggregate: its implementation, then the child cascade
    /// (<see cref="ComposeChildren"/>).
    /// </summary>
    /// <param name="header">The already-loaded header row to compose.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The composed aggregate, or the first failing step's result.</returns>
    protected Task<IGenericResult<DomainConfiguration>> ComposeAggregate(DomainConfiguration header, CancellationToken ct = default)
        => ComposeAggregate(header, null, ct);

    /// <summary>
    /// Composes a header row into its full aggregate as of a past instant.
    /// </summary>
    /// <param name="header">The already-loaded header row to compose.</param>
    /// <param name="asOf">
    /// The instant to compose as of — the typed body and children belonging to the version in force
    /// then — or null for the current ones.
    /// </param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The composed aggregate, or the first failing step's result.</returns>
    protected async Task<IGenericResult<DomainConfiguration>> ComposeAggregate(DomainConfiguration header, DateTimeOffset? asOf, CancellationToken ct)
    {
        // A domain row names an implementation and something is registered under that name: read it,
        // give it the domain's name, and attach it. A row that names nothing, or a provider with an
        // empty registry, falls straight through to the subtree -- the registry answers the question,
        // so there is nothing to ask about what kind of provider this is.
        if (header is IDomainConfiguration domain
            && domain.Implementation is { Length: > 0 } implementation
            && _implementations.TryGetValue(implementation, out var implementationProvider))
        {
            DefaultConfigurationProviderLog.LoadingTypedBody(
                _logger, typeof(DomainConfiguration).Name, domain.Name, implementation);

            var loaded = await implementationProvider.Get(domain.Id, ct).ConfigureAwait(false);
            if (!loaded.IsSuccess)
            {
                return GenericResult<DomainConfiguration>.Failure(
                    DefaultConfigurationProviderLog.TypedBodyLoadFailed(
                        _logger, new InvalidOperationException(loaded.CurrentMessage),
                        typeof(DomainConfiguration).Name, domain.Name, implementation));
            }

            if (loaded.Value is IImplementationConfiguration named)
            {
                // One name for a configured member, held on the domain row. This is the only place
                // both records are in hand, so this is where it is carried across.
                named.Name = domain.Name;
                DefaultConfigurationProviderLog.TypedBodyLoaded(
                    _logger, typeof(DomainConfiguration).Name, domain.Name, implementation);
            }
        }

        return await ComposeChildren(header, asOf, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads a row by name, optionally as of a past instant.
    /// </summary>
    /// <param name="name">The configuration's name.</param>
    /// <param name="asOf">The instant to read as of, or null for the current version.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The row, or a failure.</returns>
    protected async Task<IGenericResult<DomainConfiguration>> GetByName(string name, DateTimeOffset? asOf, CancellationToken ct = default)
    {
        var gateway = Gateway();
        if (gateway.IsFailure) return gateway.ToNewResult<DomainConfiguration>();

        var result = await gateway.Value!
            .Execute<IEnumerable<DomainConfiguration>>(Commands().Get(DataStoreName, PathName, name, asOf), Target, ct)
            .ConfigureAwait(false);
        if (!result.IsSuccess) return result.ToNewResult<DomainConfiguration>();
        return GenericResult<DomainConfiguration>.Success(result.Value?.FirstOrDefault()!);
    }

    /// <summary>
    /// Reads the version of a configuration that was in force at <paramref name="asOf"/>, by id.
    /// </summary>
    /// <param name="id">The configuration's durable logical Id.</param>
    /// <param name="asOf">The instant to read the configuration as of.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The aggregate in force at that instant, or a failure.</returns>
    public virtual async Task<IGenericResult<TImplementationConfiguration>> Get(
        Guid id, DateTimeOffset asOf, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return GenericResult<TImplementationConfiguration>.Success(default!);

        var domain = await GetHeaderById(id, asOf, ct).ConfigureAwait(false);
        if (!domain.IsSuccess) return domain.ToNewResult<TImplementationConfiguration>();

        return await Implementation(domain.Value, asOf, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public virtual async Task<IGenericResult<TImplementationConfiguration>> Get(Guid id, CancellationToken ct = default)
    {
        var domain = await GetHeaderById(id, null, ct).ConfigureAwait(false);
        if (!domain.IsSuccess) return domain.ToNewResult<TImplementationConfiguration>();

        return await Implementation(domain.Value, null, ct).ConfigureAwait(false);
    }

    // The domain row names an implementation; that name selects the provider, and the domain's own
    // Id is what the provider joins on. What it returns is the answer -- there is nothing to attach
    // it to and take back off again.
    // The domain row is the tool, not the answer: it names which implementation is configured, and
    // its Configuration is already typed to that domain's contract -- so nothing converts here.
    private async Task<IGenericResult<TImplementationConfiguration>> Implementation(
        DomainConfiguration? row, DateTimeOffset? asOf, CancellationToken ct)
    {
        if (row is null)
        {
            return GenericResult<TImplementationConfiguration>.Success(default!);
        }

        if (row.Implementation is not { Length: > 0 } implementation)
        {
            return GenericResult<TImplementationConfiguration>.Failure(
                DefaultConfigurationProviderLog.NoImplementationForTypedBody(
                    _logger, typeof(DomainConfiguration).Name, row.Name));
        }

        if (!_implementations.TryGetValue(implementation, out var provider))
        {
            return GenericResult<TImplementationConfiguration>.Failure(
                DefaultConfigurationProviderLog.NoImplementationProvider(_logger, row.Name, implementation));
        }

        var loaded = asOf is { } instant
            ? await provider.Get(row.Id, instant, ct).ConfigureAwait(false)
            : await provider.Get(row.Id, ct).ConfigureAwait(false);
        if (!loaded.IsSuccess) return loaded;

        // One name for a configured member, held on the domain row and carried across here.
        if (loaded.Value is not null)
        {
            loaded.Value.Name = row.Name;
            loaded.Value.Domain = row.Domain;
        }

        return loaded;
    }

    /// <summary>
    /// Reads the header row by id WITHOUT composing the typed body. This is also the read used when a
    /// header provider dispatches to a typed-body provider: the typed provider JOINs child→parent on the
    /// FK and filters by the parent's durable Id, then returns its row unchanged (no implementation providers).
    /// </summary>
    /// <param name="id">The configuration's durable logical Id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The header row, or a failure.</returns>
    protected Task<IGenericResult<DomainConfiguration>> GetHeaderById(Guid id, CancellationToken ct = default)
        => GetHeaderById(id, null, ct);

    /// <summary>
    /// Reads the header row by id, optionally as of a past instant.
    /// </summary>
    /// <param name="id">The configuration's durable logical Id.</param>
    /// <param name="asOf">The instant to read as of, or null for the current version.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The header row, or a failure.</returns>
    protected async Task<IGenericResult<DomainConfiguration>> GetHeaderById(Guid id, DateTimeOffset? asOf, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return GenericResult<DomainConfiguration>.Success(default!);

        var parentJoin = ResolveParentJoin();
        if (parentJoin.IsFailure) return parentJoin.ToNewResult<DomainConfiguration>();

        // An implementation is reached through its domain: the id is the DOMAIN row's, and the join
        // resolves it to this row on the parent's RowId.
        var join = parentJoin.Value!;
        var cmd = Commands().GetByParentJoin(
            DataStoreName, PathName,
            join.ChildForeignKeyColumn, join.ParentTable,
            join.ParentJoinColumn, join.ParentKeyColumn, id, asOf);

        var gateway = Gateway();
        if (gateway.IsFailure) return gateway.ToNewResult<DomainConfiguration>();

        var result = await gateway.Value!.Execute<IEnumerable<DomainConfiguration>>(cmd, Target, ct).ConfigureAwait(false);
        if (!result.IsSuccess) return result.ToNewResult<DomainConfiguration>();
        return GenericResult<DomainConfiguration>.Success(result.Value?.FirstOrDefault()!);
    }

    // lookup. Each descriptor carries the physical {Owner}RowId FK column; children are queried via the
    // gateway keyed on the owner's RowId and recursed, so any N-level aggregate composes from data rows
    // alone (DataStore→Paths→Containers→Fields, Connection→typed body→auth/limits, DataSet→Fields,
    // Escalation→levels). This is what lets a RUNTIME store — e.g. AuthDb, which lives in ConfigurationDb's
    // data.* rows and is deliberately absent from configurationSchema.json — compose its full tree.
    private async Task<IGenericResult<DomainConfiguration>> ComposeChildren(DomainConfiguration header, DateTimeOffset? asOf, CancellationToken ct)
    {
        var mapper = PocoMapperCollection.ByName(typeof(DomainConfiguration).Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult<DomainConfiguration>.Success(header);

        await LoadChildrenInto(header, mapper, Commands().TableName, asOf, ct).ConfigureAwait(false);
        return GenericResult<DomainConfiguration>.Success(header);
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
            DefaultConfigurationProviderLog.NoSuitableKeyForContainer(_logger, typeof(DomainConfiguration).Name, ownerContainerName);
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
    }

    private IDataCommand BuildChildJoinQuery(
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

        return builder
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

        var physical = FindKeyFieldName(containerResult.Value.Keys, "Physical");
        var logical = FindKeyFieldName(containerResult.Value.Keys, "Logical");
        return physical is null || logical is null ? null : (physical, logical);
    }

    /// <summary>
    /// How a child row reaches its parent: the child's foreign key column, the parent table, and the
    /// parent columns to join and filter on. <see cref="ParentJoinInfo.None"/> means no parent.
    /// </summary>
    protected sealed record ParentJoinInfo(
        bool HasParent,
        string ChildForeignKeyColumn,
        string ParentTable,
        string ParentJoinColumn,
        string ParentKeyColumn)
    {
        /// <summary>The sentinel meaning this configuration has no parent.</summary>
        public static ParentJoinInfo None { get; } =
            new(false, string.Empty, string.Empty, string.Empty, string.Empty);
    }

    /// <summary>
    /// Works out whether this configuration has a parent, and how to join to it.
    /// </summary>
    /// <returns>The parent join, or <see cref="ParentJoinInfo.None"/> when there is no parent.</returns>
    /// <remarks>
    /// The base infers this from container metadata by looking for a declared foreign key, which
    /// answers a narrower question than the one asked: a foreign key can mean the row belongs to a
    /// parent, or only that it cites a lookup. Read as a parent, a citation makes the base build a
    /// parent-join query and refuse to resolve the row by its own name.
    ///
    /// A provider is written for one configuration type, so a type that knows it has no parent should
    /// override this and say so rather than leave it to be inferred from whatever constraints happen
    /// to exist in the database.
    /// </remarks>
    protected virtual IGenericResult<ParentJoinInfo> ResolveParentJoin()
    {
        var gateway = Gateway();
        if (gateway.IsFailure) return gateway.ToNewResult<ParentJoinInfo>();

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
            return GenericResult<ParentJoinInfo>.Success(ParentJoinInfo.None);

        var pathResult = store.Path(PathName);
        if (!pathResult.IsSuccess || pathResult.Value is null)
            return GenericResult<ParentJoinInfo>.Success(ParentJoinInfo.None);

        var containerResult = pathResult.Value.Container(Commands().TableName);
        if (!containerResult.IsSuccess || containerResult.Value is null)
            return GenericResult<ParentJoinInfo>.Success(ParentJoinInfo.None);

        var fk = FindForeignKey(containerResult.Value.Keys, pathResult.Value, Commands().TableName);
        if (fk?.ReferencedContainer is null || fk.KeyFields.Count == 0)
            return GenericResult<ParentJoinInfo>.Success(ParentJoinInfo.None);

        var parent = fk.ReferencedContainer;
        var parentKeys = ResolveParentKeys(parent, pathResult.Value);
        var parentJoinColumn = FindKeyFieldName(parentKeys, "Physical");  // parent RowId — the FK target
        var parentKeyColumn = FindKeyFieldName(parentKeys, "Logical");    // parent durable Id — the filter
        if (parentJoinColumn is null || parentKeyColumn is null)
            return GenericResult<ParentJoinInfo>.Failure(
                DefaultConfigurationProviderLog.NoSuitableKeyForContainer(_logger, typeof(DomainConfiguration).Name, parent.Name));

        return GenericResult<ParentJoinInfo>.Success(new ParentJoinInfo(
            HasParent: true,
            ChildForeignKeyColumn: fk.KeyFields[0].LocalField.Name,
            ParentTable: parent.Name,
            ParentJoinColumn: parentJoinColumn,
            ParentKeyColumn: parentKeyColumn));
    }

    private static IReadOnlyList<IContainerKey> ResolveParentKeys(IDataContainer parent, IDataNodePath path)
    {
        if (parent.Keys.Count > 0)
            return parent.Keys;
        var resolved = path.Container(parent.Name);
        return resolved.IsSuccess && resolved.Value is not null ? resolved.Value.Keys : parent.Keys;
    }

    private static IContainerKey? FindForeignKey(IReadOnlyList<IContainerKey> keys, IDataNodePath path, string ownContainerName)
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

            var referencedName = key.ReferencedContainer?.Name;
            if (referencedName is null) continue;
            if (string.Equals(referencedName, ownContainerName, StringComparison.Ordinal)) continue;
            if (!path.Container(referencedName).IsSuccess) continue;

            return key;
        }
        return null;
    }

    private static string? FindKeyFieldName(IReadOnlyList<IContainerKey> keys, string keyTypeName)
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


    /// <inheritdoc/>
    /// <remarks>
    /// The domain rows are read to find what is configured, then each is resolved to the
    /// implementation it names. The domain record is not returned -- it is the lookup, not the
    /// answer -- so a caller listing a domain gets the same thing Get(name) would have given it.
    /// </remarks>
    public virtual async Task<IGenericResult<IReadOnlyList<TImplementationConfiguration>>> Get(CancellationToken ct = default)
    {
        var gateway = Gateway();
        if (gateway.IsFailure) return gateway.ToNewResult<IReadOnlyList<TImplementationConfiguration>>();

        var rows = await gateway.Value!.Execute<IEnumerable<DomainConfiguration>>(
            Commands().List(DataStoreName, PathName), Target, ct).ConfigureAwait(false);
        if (!rows.IsSuccess) return rows.ToNewResult<IReadOnlyList<TImplementationConfiguration>>();

        var found = new List<TImplementationConfiguration>();
        foreach (var row in rows.Value ?? [])
        {
            var one = await Implementation(row, null, ct).ConfigureAwait(false);
            if (!one.IsSuccess) return one.ToNewResult<IReadOnlyList<TImplementationConfiguration>>();
            if (one.Value is not null) found.Add(one.Value);
        }

        return GenericResult<IReadOnlyList<TImplementationConfiguration>>.Success(found);
    }

    /// <summary>Persists a configuration record and its whole child tree.</summary>
    /// <remarks>
    /// <para>
    /// Why: when the caller hasn't supplied an Id, mint the DURABLE Id with UUID v7 so the record can
    /// be inserted without round-tripping to the DB to assign it. The physical RowId is a DB-managed
    /// INT IDENTITY (invisible — never set here); the time-ordered durable Id keeps logical creation
    /// order stable across versions.
    /// </para>
    /// <para>
    /// THIS CASCADES UNCONDITIONALLY. Every child row in the aggregate is rewritten on every call —
    /// there is no diff and no dirty check — so using it to change one field on one child stamps
    /// ModifyDate and ModifyBy across rows nobody touched, and under version-on-write mints a new
    /// version of each. An audit trail then reports that someone edited the whole collection when
    /// they edited one row of it, which is deterministic rather than a race. Reading, mutating one
    /// child and calling this also overwrites anything another caller wrote in between.
    /// </para>
    /// <para>
    /// To change part of an aggregate, use <see cref="SaveChild{TChild}"/> or
    /// <see cref="DeleteChild{TChild}"/> and expose a named domain method over it.
    /// </para>
    /// </remarks>
    // Writes the one row this provider owns, whichever half of the pair that is. Not public, and
    // not a way to create a domain row on its own: a domain row names an implementation, so the two
    // are always written together by the create overload above.
    protected virtual async Task<IGenericResult<DomainConfiguration>> WriteRow(DomainConfiguration record, CancellationToken ct = default)
    {
        // The guard that stood here refused a domain row naming an implementation it did not carry.
        // Save writes both rows together now, so that state cannot be reached through this path.

        ArgumentNullException.ThrowIfNull(record);

        if (record.Id == Guid.Empty)
        {
            record.Id = Guid.CreateVersion7();
        }

        var gatewayForSave = Gateway();
        if (gatewayForSave.IsFailure) return gatewayForSave.ToNewResult<DomainConfiguration>();

        var result = await gatewayForSave.Value!.Execute<DomainConfiguration>(
            Commands().Create(DataStoreName, PathName, record), Target, ct).ConfigureAwait(false);
        if (!result.IsSuccess) return result;

        var cascade = await CascadeOwnerChildren(record, ct).ConfigureAwait(false);
        if (!cascade.IsSuccess) return cascade.ToNewResult<DomainConfiguration>();

        return GenericResult<DomainConfiguration>.Success(record);
    }

    /// <summary>Writes ONE child row, without touching the rest of the aggregate.</summary>
    /// <typeparam name="TChild">The child configuration type.</typeparam>
    /// <param name="child">The child row to write.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <remarks>
    /// The counterpart to <see cref="WriteRow"/>'s unconditional cascade: this writes the row it is
    /// given and nothing else, so changing one member's role leaves every other row's audit columns
    /// alone. Protected rather than public so a domain provider publishes a named operation —
    /// SetMemberRole, AttachResource — and an endpoint never handles a child-level primitive.
    ///
    /// Pass several of these inside a <see cref="BeginTransaction"/> scope when a set of rows has to
    /// land together, then call <see cref="InvalidateCache"/> after the commit.
    /// </remarks>
    protected async Task<IGenericResult> SaveChild<TChild>(
        TChild child,
        CancellationToken ct = default)
        where TChild : IGenericConfiguration
    {
        ArgumentNullException.ThrowIfNull(child);

        var result = await SaveOneChild(child, ct).ConfigureAwait(false);
        if (!result.IsSuccess) return result;

        // The cached aggregate still holds the row as it was; without this a re-read serves it.
        InvalidateCache();
        return GenericResult.Success();
    }

    /// <summary>Deletes ONE child row, without touching the rest of the aggregate.</summary>
    /// <typeparam name="TChild">The child configuration type.</typeparam>
    /// <param name="id">The child's logical identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <remarks>
    /// The delete counterpart to <see cref="SaveChild{TChild}"/>. Resolves the child's own command
    /// and container, so the delete lands on the child's table rather than the owner's.
    /// </remarks>
    protected async Task<IGenericResult> DeleteChild<TChild>(
        Guid id,
        CancellationToken ct = default)
        where TChild : IGenericConfiguration
    {
        if (id == Guid.Empty)
        {
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.ConfigurationNotFound(
                    _logger, typeof(TChild).Name, id.ToString()));
        }

        var command = ConfigurationCommands.All().FirstOrDefault(c => c.ConfigType == typeof(TChild));
        if (command is null)
        {
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.NoChildCommandForType(
                    _logger, typeof(DomainConfiguration).Name, typeof(TChild).Name));
        }

        var gateway = Gateway();
        if (gateway.IsFailure) return gateway;

        var result = await gateway.Value!.Execute(
            command.Delete(DataStoreName, PathName, id),
            new DataStoreTarget(DataStoreName, PathName, command.ContainerName),
            ct).ConfigureAwait(false);
        if (!result.IsSuccess) return result;

        InvalidateCache();
        return GenericResult.Success();
    }


    // Cascade-save the composed aggregate on EVERY write. The owner row itself is already persisted before this
    // runs (the wrapper Create for the root record; SaveOneChild for a nested typed body). There are two
    // relationship kinds and ONE recursive walk:
    //  1. Header -> typed body: the single "Configuration" property (e.g. Connection -> MsSqlConnection,
    //     Pipeline -> EtlPipeline -> BatchCopy/Streaming). The body ROW is saved, then we recurse so the
    //     body's OWN typed body + collections are cascaded as their own sub-aggregate — this is what makes
    //     a multi-level typed-body chain (Pipeline → EtlPipeline → engine) fully persist.
    //  2. Child collections: a property whose value is a list of typed configurations. These can appear on
    //     the root record (e.g. annotation Tags, dataset Fields) AND on any typed body (e.g. an ETL
    //     pipeline's Transforms), and each item can own further collections (e.g. a Transform's
    //     FieldMappings) — an N-level recursive walk.
    // Every child — at every level, on the root OR on a typed body — is linked to its IMMEDIATE owner via
    // the {Strip(owner.Type.Name)}Id logical FK set to owner.Id (e.g. transform.EtlPipelineId =
    // etlPipeline.Id, limit.MsSqlConnectionId = msSqlConnection.Id, fieldMapping.PipelineTransformId =
    // transform.Id). This is the SAME per-owner rule CascadeCollections already applies when it recurses
    // into a child's nested collections (StripConfigurationSuffix(item.Type.Name) + "Id" = item.Id); we
    // apply it uniformly to the typed body too, removing the old root-only special case. That special case
    // mis-keyed typed-body collections — it set the ROOT's FK name/Id (e.g. "PipelineId"/"ConnectionId")
    // on rows whose FK column targets the typed body ("EtlPipelineId"/"MsSqlConnectionId"), so the
    // configuration save translator could not resolve the physical RowId and the FK was never set. The
    // translator resolves the physical RowId FK by subquery on insert from the corrected logical FK.
    private async Task<IGenericResult> CascadeOwnerChildren(IGenericConfiguration owner, CancellationToken ct)
    {
        var mapper = PocoMapperCollection.ByName(owner.GetType().Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult.Success();

        // Relationship 1 — single typed-body "Configuration" property. Save the body ROW, then recurse so
        // ITS typed body + collections cascade as their own sub-aggregate (NOT double-saved here).
        var typedBody = mapper.GetTypedBody(owner);
        if (typedBody is not null)
        {
            var bodyMapper = PocoMapperCollection.ByName(typedBody.GetType().Name);
            if (bodyMapper != PocoMapperCollection.NotFound)
                bodyMapper.SetValue(typedBody, StripConfigurationSuffix(owner.GetType().Name) + "Id", owner.Id);

            {
                var bodyResult = await SaveOneChild(typedBody, ct).ConfigureAwait(false);
                if (!bodyResult.IsSuccess) return bodyResult;

                var bodyTreeResult = await CascadeOwnerChildren(typedBody, ct).ConfigureAwait(false);
                if (!bodyTreeResult.IsSuccess) return bodyTreeResult;
            }
        }

        // Relationship 2 — child collections, FK'd to THIS owner's logical identity.
        return await CascadeCollections(
            owner,
            StripConfigurationSuffix(owner.GetType().Name) + "Id",
            owner.Id,
            ct).ConfigureAwait(false);
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
                // on insert. The FK name is runtime-varying (root FK for level-1, parent-item FK deeper),
                // so a generated SetValue(name) — not a fixed typed setter — is required.
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

    // Saves a single composed child via its per-type ConfigurationCommand (one path for both
    // relationship kinds), looked up by config-TYPE identity with no reflection
    // (the non-generic IConfigurationCommands.Create / IConfigurationGateway.Execute).
    private async Task<IGenericResult> SaveOneChild(IGenericConfiguration childCfg, CancellationToken ct)
    {
        var childType = childCfg.GetType();
        var command = ConfigurationCommands.All().FirstOrDefault(c => c.ConfigType == childType);
        if (command is null)
        {
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.NoChildCommandForType(
                    _logger, typeof(DomainConfiguration).Name, childType.Name));
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

    private async Task<IGenericResult> RetireOwnerChildren(IGenericConfiguration owner, CancellationToken ct)
    {
        var mapper = PocoMapperCollection.ByName(owner.GetType().Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult.Success();

        // Reverse of relationship 2 — this owner's child collections and property bags, deepest first.
        var collections = await RetireCollections(owner, StripConfigurationSuffix(owner.GetType().Name) + "Id", owner.Id, ct).ConfigureAwait(false);
        if (!collections.IsSuccess) return collections;

        // Reverse of relationship 1 — the typed body, after everything the OWNER owned directly.
        //
        // No registered provider: this owner is a leaf, or a nested body the recursion already
        // materialized — the same distinction the save cascade makes at the same point.
        var typedBody = mapper.GetTypedBody(owner);
        if (typedBody is null)
            return GenericResult.Success();

        var bodyTree = await RetireOwnerChildren(typedBody, ct).ConfigureAwait(false);
        if (!bodyTree.IsSuccess) return bodyTree;

        return await RetireOneChild(typedBody, ct).ConfigureAwait(false);
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
    // SaveOneChild, resolved the same way (by ConfigType through All(), since ConfigurationCommands has
    // stub ByName/ById) and failing loud on the same condition.
    private async Task<IGenericResult> RetireOneChild(IGenericConfiguration childCfg, CancellationToken ct)
    {
        var childType = childCfg.GetType();
        var command = ConfigurationCommands.All().FirstOrDefault(c => c.ConfigType == childType);
        if (command is null)
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.NoChildCommandForType(
                    _logger, typeof(DomainConfiguration).Name, childType.Name));

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

    /// <summary>Soft-deletes a configuration record by Id, cascading to its whole aggregate in reverse order.</summary>
    public virtual async Task<IGenericResult> Delete(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.ConfigurationNotFound(_logger, typeof(DomainConfiguration).Name, id.ToString()));

        var header = await GetHeaderById(id, null, ct).ConfigureAwait(false);
        if (!header.IsSuccess) return header;
        if (header.Value is null)
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.ConfigurationNotFound(_logger, typeof(DomainConfiguration).Name, id.ToString()));

        var existing = await ComposeChildren(header.Value, null, ct).ConfigureAwait(false);
        if (!existing.IsSuccess) return existing;
        if (existing.Value is null)
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.ConfigurationNotFound(_logger, typeof(DomainConfiguration).Name, id.ToString()));

        var cascade = await RetireOwnerChildren(existing.Value, ct).ConfigureAwait(false);
        if (!cascade.IsSuccess) return cascade;

        var cmd = Commands().Delete(DataStoreName, PathName, existing.Value.Id);
        var gatewayForDelete = Gateway();
        if (gatewayForDelete.IsFailure) return gatewayForDelete;

        var result = await gatewayForDelete.Value!.Execute<DomainConfiguration>(cmd, Target, ct).ConfigureAwait(false);
        if (!result.IsSuccess) return result;

        return GenericResult.Success();
    }

    /// <inheritdoc/>
    public virtual async Task<IGenericResult> Delete(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(name))
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.ConfigurationNotFound(_logger, typeof(DomainConfiguration).Name, name));
        var getResult = await Get(name, ct).ConfigureAwait(false);
        if (!getResult.IsSuccess) return getResult;
        if (getResult.Value is null)
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.ConfigurationNotFound(_logger, typeof(DomainConfiguration).Name, name));
        return await Delete(getResult.Value.Id, ct).ConfigureAwait(false);
    }

    /// <summary>Opens a transaction over the store this provider reads and writes.</summary>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <remarks>
    /// Here rather than on the caller so an endpoint does not need <c>IConfigurationGateway</c> to
    /// begin one. The provider already knows which store it is bound to, so the caller cannot open
    /// a transaction against a different store than the one it then saves into — which is what
    /// passing the store name in from outside allowed.
    /// </remarks>
    public virtual async Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(
        CancellationToken ct = default)
    {
        var gateway = Gateway();

        return gateway.IsSuccess && gateway.Value is { } resolved
            ? await resolved.BeginTransaction(DataStoreName, ct).ConfigureAwait(false)
            : gateway.ToNewResult<IDataGatewayTransaction>();
    }

    /// <summary>
    /// Saves a configuration record inside the provided transaction scope.
    /// Use when the save must be atomic with other operations (e.g., security stamp bump).
    /// </summary>
    /// <remarks>
    /// Does NOT cascade child saves and does NOT invalidate the cache — the caller is
    /// responsible for cache invalidation after a successful commit.
    /// </remarks>
    public virtual async Task<IGenericResult<DomainConfiguration>> SaveInTransaction(
        DomainConfiguration record,
        IDataGatewayTransaction transaction,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.Id == Guid.Empty)
            record.Id = Guid.CreateVersion7();

        var result = await transaction.Execute<DomainConfiguration>(
            Commands().Create(DataStoreName, PathName, record), Target, ct).ConfigureAwait(false);
        return result.IsSuccess ? GenericResult<DomainConfiguration>.Success(record) : result;
    }

    /// <summary>
    /// Soft-deletes a configuration record by Id inside the provided transaction scope.
    /// Use when the delete must be atomic with other operations.
    /// </summary>
    public virtual async Task<IGenericResult> DeleteInTransaction(
        Guid id,
        IDataGatewayTransaction transaction,
        CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.ConfigurationNotFound(_logger, typeof(DomainConfiguration).Name, id.ToString()));

        var result = await transaction.Execute<DomainConfiguration>(
            Commands().Delete(DataStoreName, PathName, id), Target, ct).ConfigureAwait(false);
        return result.IsSuccess ? GenericResult.Success() : result;
    }

    /// <summary>
    /// Invalidates the cached reads for this provider's table.
    /// Call after committing a transaction whose writes used
    /// <see cref="SaveInTransaction"/> / <see cref="DeleteInTransaction"/>, since those
    /// defer to the caller's transaction and cannot invalidate before commit.
    /// </summary>
    public void InvalidateCache()
    {
        var gateway = Gateway();
        if (gateway.IsSuccess)
            gateway.Value!.InvalidateCachedResults(Target);
    }

    /// <summary>Gets the gateway onto this provider's configuration connection.</summary>
    /// <returns>The gateway, or a failure naming the connection no gateway serves.</returns>
    protected IGenericResult<IConfigurationGateway> Gateway() => _gatewayProvider.Get(DataStoreName);

    /// <summary>Runs <paramref name="call"/> against this provider's configuration gateway.</summary>
    /// <typeparam name="T">The result type the call materialises.</typeparam>
    /// <param name="call">The command and the container it targets.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The gateway's result, or the failure naming the connection no gateway serves.</returns>
    /// <remarks>
    /// Every provider reaches its store the same way, so resolving the gateway and failing when none
    /// serves the connection belongs here once rather than at each call site.
    /// </remarks>
    protected async Task<IGenericResult<T>> Execute<T>(
        DataGatewayCall call, CancellationToken ct = default)
    {
        var gateway = Gateway();
        return gateway.IsFailure
            ? gateway.ToNewResult<T>()
            : await gateway.Value!.Execute<T>(call, ct).ConfigureAwait(false);
    }


}
