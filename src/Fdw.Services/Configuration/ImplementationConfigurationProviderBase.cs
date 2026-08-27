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
using Fdw.Services.Configuration.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Configuration;

/// <summary>
/// Single-source configuration provider over the configured <see cref="IConfigurationGateway"/>
/// (ConfigurationDb). Commands come from the <c>ConfigurationCommands</c> TypeCollection keyed on
/// <typeparamref name="TCommand"/>.
/// </summary>
/// <typeparam name="TConfig">The configuration POCO type.</typeparam>
/// <typeparam name="TCommand">The configuration command TypeOption for this domain.</typeparam>
public class ImplementationConfigurationProviderBase<TConfig, TCommand>
    : IServiceConfigurationProvider<TConfig>, IServiceConfigurationProvider
    where TConfig : class, IGenericConfiguration
    where TCommand : ConfigurationCommandBase<TConfig>
{
    // Why Lazy: resolving the gateway eagerly triggers DataGatewayService →
    // ConfigurationGatewayDataStoreProvider → DataStoreConfigurationProvider, which constructs a
    // provider again — the StackGuard deadlocks on the singleton lock. Deferring to first use breaks
    // the cycle, and invalidation goes through this same Lazy so it inherits the same protection.
    private readonly IConfigurationGatewayProvider _gatewayProvider;
    private readonly ILogger _logger;
    private readonly AsyncLocal<bool> _isQuerying = new();

    /// <summary>
    /// DataStore name this provider targets (e.g. "ConfigurationDb"). Set at construction (the
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
    // Why the provider and not a gateway: the connection a domain reads is named by its collection's
    // ConfigurationConnection and is settable by a host, so which gateway serves this provider is not
    // known when the container is built. Resolving per call keeps that name authoritative.
    public ImplementationConfigurationProviderBase(
        ILogger<ImplementationConfigurationProviderBase<TConfig, TCommand>>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName)
    {
        _logger = logger ?? NullLogger<ImplementationConfigurationProviderBase<TConfig, TCommand>>.Instance;
        _gatewayProvider = gatewayProvider ?? throw new ArgumentNullException(nameof(gatewayProvider));
        DataStoreName = dataStoreName ?? throw new ArgumentNullException(nameof(dataStoreName));
        PathName = pathName ?? throw new ArgumentNullException(nameof(pathName));
    }



    /// <summary>
    /// Per-discriminator registry of typed-body providers, keyed by the framework-level
    /// <see cref="IGenericConfiguration.ServiceOptionType"/> (e.g. "MsSql", "Http"). A polymorphic HEADER
    /// provider (Connection, SecretManager, ...) registers one entry per typed body via
    /// <c>Register</c>; a leaf/child provider (e.g. the MsSqlConnectionConfiguration body
    /// provider) never registers any, so its registry stays empty — that emptiness is how
    /// <c>ComposeTypedBody</c> distinguishes a header provider from a leaf.
    /// </summary>
    protected ConcurrentDictionary<string, IServiceConfigurationProvider> ImplementationProviders { get; }
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a typed-body configuration provider for a specific <c>ServiceOptionType</c> discriminator.
    /// Called once per typed body when the domain's providers are wired, so that
    /// <see cref="Get(string,CancellationToken)"/>/<see cref="Get(Guid,CancellationToken)"/> can compose
    /// the typed body after reading the header row.
    /// </summary>
    /// <param name="serviceOptionType">The discriminator (e.g. "MsSql", "Http", "OpenIddict").</param>
    /// <param name="provider">The typed-body configuration provider for that discriminator.</param>
    public void Register(string serviceOptionType, IServiceConfigurationProvider provider)
    {
        ImplementationProviders[serviceOptionType] = provider;
        DefaultConfigurationProviderLog.TypedProviderRegistered(_logger, typeof(TConfig).Name, serviceOptionType);
    }


    // ── Type-erased surface ─────────────────────────────────────────────────
    // Why explicit: a parent provider holds its typed bodies as IServiceConfigurationProvider and only
    // ever reads, saves or deletes through them. These three delegate to the typed members, so the
    // widening happens here instead of in a per-registration forwarding adapter.

    async Task<IGenericResult<IGenericConfiguration>> IServiceConfigurationProvider.Get(Guid id, CancellationToken ct)
    {
        var result = await Get(id, ct).ConfigureAwait(false);
        return result.IsSuccess
            ? result.ToNewResult<IGenericConfiguration>(result.Value!)
            : result.ToNewResult<IGenericConfiguration>();
    }

    async Task<IGenericResult<IGenericConfiguration>> IServiceConfigurationProvider.Get(string name, CancellationToken ct)
    {
        var result = await Get(name, ct).ConfigureAwait(false);
        return result.IsSuccess
            ? result.ToNewResult<IGenericConfiguration>(result.Value!)
            : result.ToNewResult<IGenericConfiguration>();
    }

    async Task<IGenericResult> IServiceConfigurationProvider.Save(IGenericConfiguration record, CancellationToken ct)
    {
        if (record is not TConfig typed)
        {
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.UntypedSaveTypeMismatch(
                    _logger, typeof(TConfig).Name, record?.GetType().Name ?? "null"));
        }

        return await Save(typed, ct).ConfigureAwait(false);
    }

    // Why: TCommand is a [TypeOption] in ConfigurationCommands — the source generator registers it
    // at static-ctor time and it's guaranteed present by the time this lambda runs.
    private static readonly Lazy<TCommand> _commands = new(static () =>
        ConfigurationCommands.All().OfType<TCommand>().Single());

    /// <summary>Returns the TCommand TypeOption instance for this domain.</summary>
    protected TCommand Commands() => _commands.Value;

    // Why: the addressing target for this provider's bound table — DataStore + Path + TableName.
    // The gateway's target-typed Execute reads addressing from here, not from the command. The
    // command no longer needs to carry addressing once the strip lands; this is the single source.
    private DataStoreTarget Target => new(DataStoreName, PathName, Commands().TableName);

    /// <inheritdoc/>
    // Why: reads the header row by name, then composes the polymorphic typed body uniformly via
    // ComposeTypedBody — the read mirror of CascadeChildSave's typed-body save. Leaf/child providers
    // (no implementation providers) pass through ComposeTypedBody unchanged; header providers attach the body.
    public virtual async Task<IGenericResult<TConfig>> Get(string name, CancellationToken ct = default)
    {
        var headerResult = await GetHeaderByName(name, null, ct).ConfigureAwait(false);
        if (!headerResult.IsSuccess || headerResult.Value is null) return headerResult;
        return await ComposeAggregate(headerResult.Value, null, ct).ConfigureAwait(false);
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
    public virtual async Task<IGenericResult<TConfig>> GetAsOf(string name, DateTimeOffset asOf, CancellationToken ct = default)
    {
        var headerResult = await GetHeaderByName(name, asOf, ct).ConfigureAwait(false);
        if (!headerResult.IsSuccess || headerResult.Value is null) return headerResult;
        return await ComposeAggregate(headerResult.Value, asOf, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Composes a header row into its full aggregate: the polymorphic typed body (<see cref="ComposeTypedBody"/>)
    /// followed by the child cascade (<see cref="ComposeChildren"/>).
    /// </summary>
    /// <param name="header">The already-loaded header row to compose.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The composed aggregate, or the first failing step's result.</returns>
    // Why (FDW-558): the single "header -> full aggregate" compose step (typed body + child cascade),
    // shared by the by-name/by-id single-record reads AND (via a domain override) the all-items list
    // read. Extracting this stops a domain from having to duplicate ComposeTypedBody+ComposeChildren
    // itself just to make its Get(CancellationToken) return composed rows instead of bare headers.
    // Why the overload pair: domain providers already call this positionally as (header, ct) to
    // compose their list reads, so the current-version shape has to survive adding the temporal one.
    protected Task<IGenericResult<TConfig>> ComposeAggregate(TConfig header, CancellationToken ct = default)
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
    protected async Task<IGenericResult<TConfig>> ComposeAggregate(TConfig header, DateTimeOffset? asOf, CancellationToken ct)
    {
        var typedResult = await ComposeTypedBody(header, asOf, ct).ConfigureAwait(false);
        if (!typedResult.IsSuccess || typedResult.Value is null) return typedResult;
        return await ComposeChildren(typedResult.Value, asOf, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the header row by name WITHOUT composing the typed body. Use for management flows
    /// (delete, exists-check) that need only the header and must not fail when the header's
    /// discriminator has no registered typed provider.
    /// </summary>
    /// <param name="name">The configuration's name.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The header row, or a failure.</returns>
    /// <remarks>
    /// Why an overload pair rather than one method with an optional asOf: CA1068 requires the
    /// CancellationToken to be the last parameter, so threading asOf into this signature would have
    /// had to push ct along — silently breaking every positional caller and every domain provider
    /// that subclasses this. The current-version read keeps its exact shape.
    /// </remarks>
    // Why: name-based lookup emits WHERE [Name]=@p0 against the bound table. Tables with no
    // Name column (e.g. typed-body tables like conn.MsSqlConnection) MUST NOT be looked up by
    // name — callers must use Get(Guid id) with the parent's logical Id instead, which walks
    // the IDataStore tree and emits WHERE [ConnectionId]=@p0 from the container's FK key.
    protected Task<IGenericResult<TConfig>> GetHeaderByName(string name, CancellationToken ct = default)
        => GetHeaderByName(name, null, ct);

    /// <summary>
    /// Reads the header row by name, optionally as of a past instant.
    /// </summary>
    /// <param name="name">The configuration's name.</param>
    /// <param name="asOf">The instant to read as of, or null for the current version.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The header row, or a failure.</returns>
    protected async Task<IGenericResult<TConfig>> GetHeaderByName(string name, DateTimeOffset? asOf, CancellationToken ct = default)
    {
        // Why: a typed-body table (declared parent FK) has no Name column — a name lookup would emit
        // WHERE [Name]=@p0 and fail with a raw "Invalid column name 'Name'" SQL error. Fail loud with a
        // structured message; the caller must resolve by parent Id (Get(Guid)). Read mirror of
        // GetHeaderById's parent-join dispatch. NO FALLBACKS WITHOUT EXPLICIT APPROVAL.
        var parentJoin = ResolveParentJoin();
        if (parentJoin.IsFailure) return parentJoin.ToNewResult<TConfig>();
        if (parentJoin.Value!.HasParent)
            return GenericResult<TConfig>.Failure(
                DefaultConfigurationProviderLog.TypedBodyNotResolvableByName(
                    _logger, typeof(TConfig).Name, Commands().TableName, name));

        var cmd = Commands().Get(DataStoreName, PathName, name, asOf);
        var gateway = Gateway();
        if (gateway.IsFailure) return gateway.ToNewResult<TConfig>();

        var result = await gateway.Value!.Execute<IEnumerable<TConfig>>(cmd, Target, ct).ConfigureAwait(false);
        if (!result.IsSuccess) return result.ToNewResult<TConfig>();
        return GenericResult<TConfig>.Success(result.Value?.FirstOrDefault()!);
    }

    /// <summary>
    /// Reads the version of a configuration that was in force at <paramref name="asOf"/>, by id.
    /// </summary>
    /// <param name="id">The configuration's durable logical Id.</param>
    /// <param name="asOf">The instant to read the configuration as of.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The aggregate in force at that instant, or a failure.</returns>
    public virtual async Task<IGenericResult<TConfig>> GetAsOf(Guid id, DateTimeOffset asOf, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return GenericResult<TConfig>.Success(default!);
        var headerResult = await GetHeaderById(id, asOf, ct).ConfigureAwait(false);
        if (!headerResult.IsSuccess || headerResult.Value is null) return headerResult;
        return await ComposeAggregate(headerResult.Value, asOf, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    // Why: Get(Guid id) dispatches on ResolveParentJoin (reads the FK from container metadata).
    // Root tables (no parent FK) → the no-join sentinel → standard WHERE [Id]=@p0 (id is the row's
    // own durable Id). Typed-body tables (e.g. conn.MsSqlConnection) → JOIN child→parent on the FK
    // and filter by the parent's durable Id (id is the parent's Id). The parent's RowId is never
    // materialized — the join resolves the RowId↔RowId match. NO FALLBACKS WITHOUT EXPLICIT APPROVAL.
    public virtual async Task<IGenericResult<TConfig>> Get(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return GenericResult<TConfig>.Success(default!);
        var headerResult = await GetHeaderById(id, null, ct).ConfigureAwait(false);
        if (!headerResult.IsSuccess || headerResult.Value is null) return headerResult;
        return await ComposeAggregate(headerResult.Value, null, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the header row by id WITHOUT composing the typed body. This is also the read used when a
    /// header provider dispatches to a typed-body provider: the typed provider JOINs child→parent on the
    /// FK and filters by the parent's durable Id, then returns its row unchanged (no implementation providers).
    /// </summary>
    /// <param name="id">The configuration's durable logical Id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The header row, or a failure.</returns>
    protected Task<IGenericResult<TConfig>> GetHeaderById(Guid id, CancellationToken ct = default)
        => GetHeaderById(id, null, ct);

    /// <summary>
    /// Reads the header row by id, optionally as of a past instant.
    /// </summary>
    /// <param name="id">The configuration's durable logical Id.</param>
    /// <param name="asOf">The instant to read as of, or null for the current version.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The header row, or a failure.</returns>
    protected async Task<IGenericResult<TConfig>> GetHeaderById(Guid id, DateTimeOffset? asOf, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return GenericResult<TConfig>.Success(default!);

        // Why: typed-body tables (a declared FK to a parent) are read by JOINing the child to the
        // parent on the FK and filtering by the parent's durable Id (id) — the parent's RowId is
        // never materialized, so we cannot filter the child by RowId directly. Root tables (no FK)
        // look up by their own [Id]. The join column names are read from metadata here and passed
        // explicitly to the command verb. A resolution failure (Failure) is propagated — never a
        // silent fall-through to the [Id] path.
        var parentJoin = ResolveParentJoin();
        if (parentJoin.IsFailure) return parentJoin.ToNewResult<TConfig>();

        var join = parentJoin.Value!;
        var cmd = join.HasParent
            ? Commands().GetByParentJoin(
                DataStoreName, PathName,
                join.ChildForeignKeyColumn, join.ParentTable,
                join.ParentJoinColumn, join.ParentKeyColumn, id, asOf)
            : Commands().Get(DataStoreName, PathName, id, asOf);

        var gateway = Gateway();
        if (gateway.IsFailure) return gateway.ToNewResult<TConfig>();

        var result = await gateway.Value!.Execute<IEnumerable<TConfig>>(cmd, Target, ct).ConfigureAwait(false);
        if (!result.IsSuccess) return result.ToNewResult<TConfig>();
        return GenericResult<TConfig>.Success(result.Value?.FirstOrDefault()!);
    }

    // Why: the read mirror of CascadeChildSave's typed-body save. After the header row is read, dispatch
    // on the framework-level discriminator (IGenericConfiguration.ServiceOptionType) to the registered
    // typed provider, load the typed body row by the header's durable Id (the typed provider JOINs
    // child→parent on the FK and filters by the parent Id inside GetHeaderById), and attach it to the
    // header via the generated mapper's SetTypedBody — reflection-free, exactly as the per-domain
    // PopulateTypedBody overrides used to do. This single base path replaces every domain's copy.
    private async Task<IGenericResult<TConfig>> ComposeTypedBody(TConfig header, DateTimeOffset? asOf, CancellationToken ct)
    {
        // Why: only polymorphic HEADER providers (those with registered typed-body providers) compose a
        // typed body. A leaf/child provider — including the typed-body provider itself, whose row IS a
        // typed body and carries its own ServiceOptionType (e.g. MsSqlConnectionConfiguration => "MsSql")
        // — has an empty registry and MUST return its row unchanged. This emptiness check is what keeps
        // the dispatch from recursing into a typed provider trying to compose its own (non-existent) body.
        if (ImplementationProviders.IsEmpty)
            return GenericResult<TConfig>.Success(header);

        if (string.IsNullOrEmpty(header.ServiceOptionType))
        {
            // Why: a header with no discriminator cannot resolve a typed body. This is not fatal for a
            // pure header lookup (e.g. listing) — return the header as-is. Debug, not error (as before).
            DefaultConfigurationProviderLog.NoServiceOptionTypeForTypedBody(_logger, typeof(TConfig).Name, header.Name);
            return GenericResult<TConfig>.Success(header);
        }

        if (!ImplementationProviders.TryGetValue(header.ServiceOptionType, out var typedProvider))
        {
            return GenericResult<TConfig>.Failure(
                DefaultConfigurationProviderLog.NoImplementationProvider(
                    _logger, header.Name, header.ServiceOptionType));
        }

        DefaultConfigurationProviderLog.LoadingTypedBody(_logger, typeof(TConfig).Name, header.Name, header.ServiceOptionType);

        // Why: Pass header.Id (the parent's DURABLE key, materialized on the header — RowId is not
        // projected). The typed provider's GetHeaderById JOINs the typed body to the parent table on the
        // FK from metadata and filters by the parent's durable Id; the RowId↔RowId match is resolved
        // inside the join, so no RowId ever has to be materialized on the header object.
        var typedResult = await typedProvider.Get(header.Id, ct).ConfigureAwait(false);
        if (!typedResult.IsSuccess)
            return GenericResult<TConfig>.Failure(
                DefaultConfigurationProviderLog.TypedBodyLoadFailed(
                    _logger, new InvalidOperationException(typedResult.CurrentMessage),
                    typeof(TConfig).Name, header.Name, header.ServiceOptionType));

        // Why: the generated mapper assigns the typed body to the header's "Configuration" property with
        // no reflection. A missing mapper is non-fatal — return the header with the body unattached
        // rather than failing the read (matches the cascade-save behaviour for a missing mapper).
        var mapper = PocoMapperCollection.ByName(typeof(TConfig).Name);
        if (mapper == PocoMapperCollection.NotFound)
        {
            DefaultConfigurationProviderLog.NoMapperForTypedBody(_logger, typeof(TConfig).Name, header.Name);
            return GenericResult<TConfig>.Success(header);
        }

        mapper.SetTypedBody(header, typedResult.Value);
        DefaultConfigurationProviderLog.TypedBodyLoaded(_logger, typeof(TConfig).Name, header.Name, header.ServiceOptionType);
        return GenericResult<TConfig>.Success(header);
    }
    // lookup. Each descriptor carries the physical {Owner}RowId FK column; children are queried via the
    // gateway keyed on the owner's RowId and recursed, so any N-level aggregate composes from data rows
    // alone (DataStore→Paths→Containers→Fields, Connection→typed body→auth/limits, DataSet→Fields,
    // Escalation→levels). This is what lets a RUNTIME store — e.g. AuthDb, which lives in ConfigurationDb's
    // data.* rows and is deliberately absent from configurationSchema.json — compose its full tree.
    private async Task<IGenericResult<TConfig>> ComposeChildren(TConfig header, DateTimeOffset? asOf, CancellationToken ct)
    {
        var mapper = PocoMapperCollection.ByName(typeof(TConfig).Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult<TConfig>.Success(header);

        await LoadChildrenInto(header, mapper, Commands().TableName, asOf, ct).ConfigureAwait(false);
        return GenericResult<TConfig>.Success(header);
    }

    // Why: load every cascade child of one owner row, then recurse so nested trees materialize. RowId is
    // INVISIBLE to the app (DB-managed IDENTITY, never a POCO property), so children are NOT filtered by a
    // materialized owner RowId. Each child set is read by JOINing the child to the owner ON
    // owner.{PhysicalKey} = child.{Owner}RowId and filtering by the owner's DURABLE Id (which IS
    // materialized) — the RowId↔RowId match is resolved entirely in the DB. The owner's physical/logical
    // key COLUMN NAMES come from the container key metadata, never hardcoded.
    private async Task LoadChildrenInto(object ownerRow, IPocoMapper ownerMapper, string ownerContainerName, DateTimeOffset? asOf, CancellationToken ct)
    {
        var descriptors = ownerMapper.CascadeChildren;
        if (descriptors.Count == 0)
            return;

        // Why: the owner's durable Id is the only key the app holds (RowId is never materialized). An empty
        // Id means the row was not materialized as expected — skip rather than emit a WHERE Id=Guid.Empty.
        if (!ownerMapper.MapToParameters(ownerRow).TryGetValue("Id", out var idObj) ||
            idObj is not Guid ownerId || ownerId == Guid.Empty)
            return;

        // Why: the JOIN needs the owner's physical key column (the {Owner}RowId FK target, e.g. RowId) and
        // its durable-Id filter column (e.g. Id), read from the owner container's key metadata. If the
        // container is not locatable in the schema tree we cannot derive the join — skip the children rather
        // than guess column names (NO hardcoded "RowId"/"Id").
        var keys = ResolveOwnerKeyColumns(ownerContainerName);
        if (keys is null)
        {
            DefaultConfigurationProviderLog.NoSuitableKeyForContainer(_logger, typeof(TConfig).Name, ownerContainerName);
            return;
        }

        for (var i = 0; i < descriptors.Count; i++)
            await LoadChild(ownerRow, ownerContainerName, keys.Value.Physical, keys.Value.Logical, ownerId, descriptors[i], asOf, ct).ConfigureAwait(false);
    }

    // Why: load one cascade child off its generated descriptor. KVP property bags carry their table on the
    // descriptor (ChildContainerName from [ConfigurationChildTable]); typed-list children resolve their
    // table at runtime from the child type's ConfigurationCommand. The FK column is the descriptor's
    // physical {Owner}RowId — the JOIN ON column, not a value read off the owner POCO.
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

    // Why: KVP property bag — JOIN the child Name/Value rows to the owner and fill the parent's
    // IDictionary<string,string?> via the generated descriptor. KeyValueRow is a fixed type, so the generic
    // Execute<T> closes on it with no reflection. The child table comes from the descriptor's
    // ChildContainerName ([ConfigurationChildTable]); an empty name means the bag is unwired — skip (NO FALLBACKS).
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

    // Why: typed-config list — JOIN the child rows to the owner via the by-type Execute (the child element
    // type is known only at runtime), build the strongly-typed list through the child mapper's CreateList(),
    // assign it through the descriptor's typed setter, then recurse via the CHILD mapper (passing the child's
    // container so the next level's join resolves against the right owner table). Symmetric to the write
    // cascade in CascadeCollections. The child table is resolved from the child type's ConfigurationCommand.
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

        // Why: a typed-list child with no registered ConfigurationCommand cannot be addressed (we can't
        // name its table), so it cannot be read. Skip + log rather than fail the whole aggregate read; the
        // write side (SaveOneChild) fails loud on the same condition.
        var command = ConfigurationCommands.All().FirstOrDefault(c => c.ConfigType == descriptor.ChildType);
        if (command is null)
        {
            DefaultConfigurationProviderLog.ChildBindingSkippedNoDescriptor(
                _logger, descriptor.BoundPropertyName, descriptor.ChildTypeName, ownerRow.GetType().Name);
            return;
        }

        // Why: an [NotMapped], app-filled typed-list (e.g. BatchCopyPipelineConfiguration.Transforms, which
        // the pipeline factory copies from the kind body) gets a generated cascade descriptor whose FK
        // column is {Owner}RowId — but those rows actually FK to a DIFFERENT owner table (pipe.PipelineOperation
        // → EtlPipelineRowId, not BatchCopyPipelineRowId). Issuing the JOIN would hit SQL 207 (invalid column).
        // Skip when the child container is locatable AND its (non-empty) field set provably lacks the FK
        // column; proceed on any uncertainty so a legitimate cascade is never skipped.
        if (ChildContainerLacksColumn(command.ContainerName, fkColumn))
        {
            DefaultConfigurationProviderLog.ChildBindingSkippedNoDescriptor(
                _logger, descriptor.BoundPropertyName, descriptor.ChildTypeName, ownerRow.GetType().Name);
            return;
        }

        var cmd = BuildChildJoinQuery(command.ContainerName, fkColumn, ownerContainer, ownerPhysicalCol, ownerLogicalCol, ownerId, asOf);
        var target = new DataStoreTarget(DataStoreName, PathName, command.ContainerName);
        var gateway = Gateway();
        if (gateway.IsFailure)
            return;

        var rowsResult = await gateway.Value!.Execute(cmd, target, descriptor.ChildType, ct).ConfigureAwait(false);
        if (!rowsResult.IsSuccess || rowsResult.Value is null)
            return;

        var typedList = childMapper.CreateList();
        foreach (var item in rowsResult.Value)
            typedList.Add(item);

        descriptor.SetCollection(ownerRow, typedList);

        // Why: each loaded child is itself a parent whose own cascade descriptors drive the next level; the
        // child's own container is the owner table for that level's join.
        foreach (var item in typedList)
        {
            if (item is not null)
                await LoadChildrenInto(item, childMapper, command.ContainerName, asOf, ct).ConfigureAwait(false);
        }
    }

    // Why: the child read as a metadata-driven JOIN — child JOIN owner ON owner.{physical}=child.{fk},
    // filtered by the owner's CURRENT durable Id. RowId is never materialized; the RowId↔RowId match is
    // resolved in the DB. Mirrors ConfigurationCommandBase.GetByParentJoin (the typed-body read) for the
    // 1:N child case. The element type is irrelevant to the SQL (object); the gateway sets it per call.
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

        // Why swapping the OWNER's predicate is sufficient to make the whole cascade as-of: children
        // FK to the owner's version-specific RowId, so selecting the owner row that was in force at
        // the instant automatically selects exactly the children that belonged to that version. The
        // child rows need no temporal predicate of their own — and must not get one, since a child
        // table is not required to be temporal just because its parent is.
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

    // Why: the owner container's physical key column (the {Owner}RowId FK target, e.g. RowId) and durable-Id
    // filter column (e.g. Id), read from the ConfigurationDb schema metadata (the bounded set these providers
    // target). Returns null when the container is not locatable (partial/bootstrap tree) so the caller skips
    // rather than guessing column names — RowId is known ONLY via key metadata, never hardcoded.
    // Why: true ONLY when the child container is locatable in the bounded ConfigurationDb schema AND
    // exposes a non-empty field set that does NOT contain the cascade FK column — i.e. the descriptor's
    // {Owner}RowId FK is provably bogus for this child shape (an app-filled [NotMapped] list). Returns
    // false (caller proceeds with the read) on ANY uncertainty — container/path/fields not resolvable —
    // so a legitimate cascade whose schema we cannot see is never skipped.
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

        var fields = containerResult.Value.Schema?.Fields;
        if (fields is null || fields.Count == 0)
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

    // Why: the join descriptor for a typed-body read. All four column names are read from the
    // container metadata — NEVER hardcoded or string-stripped. ParentJoinColumn is the parent's
    // physical PK (the FK target, e.g. RowId); ParentKeyColumn is the parent's durable-Id column
    // (the filter, e.g. Id). HasParent=false is the no-join sentinel → the caller uses the [Id] path.
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
        // Why: the sentinel for a root table (no parent FK) — the caller branches to the [Id] path.
        /// <summary>The sentinel meaning this configuration has no parent.</summary>
        public static ParentJoinInfo None { get; } =
            new(false, string.Empty, string.Empty, string.Empty, string.Empty);
    }

    // Why: resolves the full child→parent join from the bounded ConfigurationDb schema metadata
    // exposed by IConfigurationGateway.DataStores (the only set FK resolution ever needs — these
    // providers always target DataStoreName = "ConfigurationDb"). The child's declared FK gives the
    // FK column + the parent container; the parent's OWN keys give the join column (Physical key) and
    // the durable-Id filter column (Logical key).
    // <para>
    // Three outcomes: (1) a resolved join (Success + value); (2) the no-join sentinel (Success +
    // <see cref="ParentJoinInfo.None"/>) → the caller uses the [Id] path — this covers a genuine root
    // table (no parent FK) AND a container that is not present in the (bootstrap/partial) schema tree,
    // so there is no metadata from which to derive a parent join; (3) a hard Failure (with
    // MessageLogging, never a silent fall-through) when the container IS present and declares a parent
    // FK but the parent's join/filter keys cannot be resolved — that is a configuration defect.
    // </para>
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
        // Why: container not locatable in the schema tree → no metadata to derive a parent join → use
        // the [Id] path (the root-table read). This is NOT a silent fallback to a wrong query: it is
        // the correct read for a table whose FK metadata is unavailable here (bootstrap/partial tree).
        if (store is null)
            return GenericResult<ParentJoinInfo>.Success(ParentJoinInfo.None);

        var pathResult = store.Path(PathName);
        if (!pathResult.IsSuccess || pathResult.Value is null)
            return GenericResult<ParentJoinInfo>.Success(ParentJoinInfo.None);

        var containerResult = pathResult.Value.Container(Commands().TableName);
        if (!containerResult.IsSuccess || containerResult.Value is null)
            return GenericResult<ParentJoinInfo>.Success(ParentJoinInfo.None);

        var fk = FindForeignKey(containerResult.Value.Keys, pathResult.Value, Commands().TableName);
        // Why: container located, no parent FK declared → genuine root table → [Id] path.
        if (fk?.ReferencedContainer is null || fk.KeyFields.Count == 0)
            return GenericResult<ParentJoinInfo>.Success(ParentJoinInfo.None);

        // Why: container located AND a parent FK is declared, but the parent's join/filter keys cannot
        // be resolved — a configuration defect. Fail loud (never fall through to the wrong [Id] query).
        var parent = fk.ReferencedContainer;
        var parentKeys = ResolveParentKeys(parent, pathResult.Value);
        var parentJoinColumn = FindKeyFieldName(parentKeys, "Physical");  // parent RowId — the FK target
        var parentKeyColumn = FindKeyFieldName(parentKeys, "Logical");    // parent durable Id — the filter
        if (parentJoinColumn is null || parentKeyColumn is null)
            return GenericResult<ParentJoinInfo>.Failure(
                DefaultConfigurationProviderLog.NoSuitableKeyForContainer(_logger, typeof(TConfig).Name, parent.Name));

        return GenericResult<ParentJoinInfo>.Success(new ParentJoinInfo(
            HasParent: true,
            ChildForeignKeyColumn: fk.KeyFields[0].LocalField.Name,
            ParentTable: parent.Name,
            ParentJoinColumn: parentJoinColumn,
            ParentKeyColumn: parentKeyColumn));
    }

    // Why: fk.ReferencedContainer is the builder's Wave-A "bare" cross-reference node, built BEFORE keys
    // are resolved (DataStoreBuilderBase wires FKs to keyless bare nodes for stable identity), so its
    // .Keys is empty. Re-resolve the parent from the path tree to read its fully-built Physical/Logical
    // keys — a typed-body chain's parent always lives in the child's own path. Extracted to keep
    // ResolveParentJoin under the FDW007 cyclomatic-complexity threshold.
    private static IReadOnlyList<IContainerKey> ResolveParentKeys(IDataContainer parent, IDataNodePath path)
    {
        if (parent.Keys.Count > 0)
            return parent.Keys;
        var resolved = path.Container(parent.Name);
        return resolved.IsSuccess && resolved.Value is not null ? resolved.Value.Keys : parent.Keys;
    }

    // Why: the parent-relationship FK. Cross-cutting FKs (TenantRowId, VisibilityGroupRowId) are
    // excluded — they exist on every managed table. The decisive selector is the path: the typed-body
    // PARENT lives in the SAME path (the version-on-write chain, e.g. pipe.BatchCopyPipeline →
    // pipe.EtlPipeline → pipe.Pipeline). A foreign key whose referenced container is NOT in this path
    // is a DATA reference (e.g. BatchCopyPipeline → data.DataSet source/sink), never the parent — skip
    // it so the parent join is selected unambiguously when a typed body carries several FKs. Returns
    // null for root tables (no parent FK in this path).
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

            // Why: select the FK whose referenced container is in THIS path (the parent), not a
            // cross-path data reference. path.Container resolves only same-path containers.
            var referencedName = key.ReferencedContainer?.Name;
            if (referencedName is null) continue;
            // Why: a self-referencing FK (e.g. authz.Role.ParentRoleRowId → Role) is a hierarchy link,
            // NOT a header→typed-body parent. Without this skip, a named root header that carries a
            // self-parent hierarchy is misclassified as a parented typed-body and GetHeaderByName
            // refuses to resolve it (TypedBodyNotResolvableByName) → Get(name) 404s. FDW-601.
            if (string.Equals(referencedName, ownContainerName, StringComparison.Ordinal)) continue;
            if (!path.Container(referencedName).IsSuccess) continue;

            return key;
        }
        return null;
    }

    // Why: the first field of the named key type on a container. For "Physical" this is the RowId PK;
    // for "Logical" this is the table's own durable Id (a Logical key that REFERENCES another
    // container is an FK-as-logical, not the table's own Id, so it is skipped).
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
    // Why (FDW-558): deliberately header-only (no ComposeTypedBody/ComposeChildren) — other domains
    // (e.g. lineage) rely on the all-items list being a cheap flat read. A domain whose list DTO needs
    // composed children (e.g. DataStore's Path/Container counts) overrides this method and calls the
    // protected ComposeAggregate hook per row — do NOT change this base behavior globally.
    public virtual async Task<IGenericResult<IReadOnlyList<TConfig>>> Get(CancellationToken ct = default)
    {
        var cmd = Commands().List(DataStoreName, PathName);
        var gateway = Gateway();
        if (gateway.IsFailure) return gateway.ToNewResult<IReadOnlyList<TConfig>>();

        var result = await gateway.Value!.Execute<IEnumerable<TConfig>>(cmd, Target, ct).ConfigureAwait(false);
        if (!result.IsSuccess) return result.ToNewResult<IReadOnlyList<TConfig>>();
        return GenericResult<IReadOnlyList<TConfig>>.Success(result.Value?.ToList() ?? []);
    }

    /// <summary>Persists a configuration record (INSERT for new, UPDATE for existing by Id).</summary>
    /// <remarks>
    /// Why: when the caller hasn't supplied an Id, mint the DURABLE Id with UUID v7 so the record can
    /// be inserted without round-tripping to the DB to assign it. The physical RowId is a DB-managed
    /// INT IDENTITY (invisible — never set here); the time-ordered durable Id keeps logical creation
    /// order stable across versions.
    /// </remarks>
    public virtual async Task<IGenericResult<TConfig>> Save(TConfig record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.Id == Guid.Empty)
        {
            record.Id = Guid.CreateVersion7();
        }

        // Why: a polymorphic header IS its typed body — the two rows are ONE aggregate. Versioning the
        // header mints a new RowId and every child FKs to that version-specific RowId, so a header saved
        // WITHOUT its body would leave the previous body attached to the retired version, where the
        // current-version read can never see it again. Fail loud rather than persist half an aggregate:
        // silently carrying the old body forward would hide exactly the header/body mismatch this
        // dispatch exists to catch (NO FALLBACKS).
        var completeness = RequireCompleteAggregate(record);
        if (!completeness.IsSuccess) return completeness.ToNewResult<TConfig>();

        // Why: ONE write path for every configuration row. ConfigurationSaveCommand IS version-on-write —
        // its translator emits "UPDATE ... SET IsCurrent=0 WHERE Id=@LogicalId AND IsCurrent=1" followed by
        // the INSERT — so it is already correct for the first write (the UPDATE matches no rows) AND every
        // later one. The branch this replaces picked a plain in-place UpdateCommand whenever a probe said
        // "exists", which wrote no new version, left IsCurrent/IsDeleted untouched, and skipped the cascade.
        // Two incompatible writes, selected by Get(record.Id) — whose meaning is NOT the same for every
        // provider: on a typed-body provider that id is the PARENT's (see Get(Guid)), so the probe never
        // matched and typed bodies only ever took the version-on-write path. Headers took the other one, so
        // a header kept a single row forever while its own body versioned underneath it. Header and body now
        // use the identical machinery, which is the entire point of the mechanism.
        var gatewayForSave = Gateway();
        if (gatewayForSave.IsFailure) return gatewayForSave.ToNewResult<TConfig>();

        var result = await gatewayForSave.Value!.Execute<TConfig>(
            Commands().Create(DataStoreName, PathName, record), Target, ct).ConfigureAwait(false);
        if (!result.IsSuccess) return result;

        // Why: the cascade runs on EVERY write, not just the first. Children FK to the owner's
        // version-specific RowId, so each new owner version needs its children re-pointed at it; the
        // previous children stay attached to the previous version, which is precisely the snapshot the
        // as-of read expects to find there.
        var cascade = await CascadeOwnerChildren(record, ct).ConfigureAwait(false);
        if (!cascade.IsSuccess) return cascade.ToNewResult<TConfig>();

        return GenericResult<TConfig>.Success(record);
    }

    // Why: the aggregate-completeness gate for the single write path. Only a POLYMORPHIC header — one whose
    // registry actually holds a provider for this record's discriminator — has a typed body that can be
    // missing; leaf rows and nested bodies have no entry and pass straight through. A discriminator with no
    // registered provider is deliberately NOT treated as incomplete here: that is the same header-vs-leaf
    // distinction ComposeTypedBody makes on the read side, and domains that legitimately allow it (see
    // special-cased in the writer.
    private IGenericResult RequireCompleteAggregate(TConfig record)
    {
        if (string.IsNullOrEmpty(record.ServiceOptionType)
            || !ImplementationProviders.ContainsKey(record.ServiceOptionType))
            return GenericResult.Success();

        var mapper = PocoMapperCollection.ByName(record.GetType().Name);
        if (mapper == PocoMapperCollection.NotFound || mapper.GetTypedBody(record) is not null)
            return GenericResult.Success();

        return GenericResult.Failure(
            DefaultConfigurationProviderLog.TypedBodyMissingOnSave(
                _logger, typeof(TConfig).Name, record.Name, record.ServiceOptionType));
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
    // Why: one recursive walk for both the root record and every typed body. The owner's own row is already
    // saved; here we save its 1:1 typed body (and recurse into that body's subtree) then cascade the
    // owner's child collections FK'd to the owner's own logical identity. For the root this is identical to
    // the previous behaviour (Strip(owner.Type.Name)+"Id" == the old rootFkName, owner.Id == record.Id);
    // for a typed body it now correctly keys to the body's identity instead of the root's.
    private async Task<IGenericResult> CascadeOwnerChildren(IGenericConfiguration owner, CancellationToken ct)
    {
        // Why: the owner's generated mapper supplies the typed body and the child-cascade descriptors with
        // NO reflection (replacing GetProperty("Configuration") + the reflective collection scan).
        var mapper = PocoMapperCollection.ByName(owner.GetType().Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult.Success();

        // Relationship 1 — single typed-body "Configuration" property. Save the body ROW, then recurse so
        // ITS typed body + collections cascade as their own sub-aggregate (NOT double-saved here).
        var typedBody = mapper.GetTypedBody(owner);
        if (typedBody is not null)
        {
            // Why: stamp the typed body's logical parent FK to THIS owner's Id using the SAME name
            // convention applied to collection children (Strip(owner.Type.Name)+"Id"), so the save
            // translator resolves the physical RowId by subquery on insert. This is what lets a
            // multi-level chain stamp the DEEPER FK (engineBody.EtlPipelineId = etlPipeline.Id): the
            // owning domain provider cannot reach a nested body's identity, which is minted mid-cascade,
            // and the body's identity isn't known until its parent body row is saved. owner.Id is always
            // materialized here (the root by Save; a nested body by its parent's SaveOneChild before this
            // recursion). SetValue no-ops when the typed body has no such column, so domains whose FK name
            // diverges are unaffected; where it matches (Connection→ConnectionId, CalculationEntity→
            // CalculationEntityId) it stamps the same value the domain's Save override already sets.
            var bodyMapper = PocoMapperCollection.ByName(typedBody.GetType().Name);
            if (bodyMapper != PocoMapperCollection.NotFound)
                bodyMapper.SetValue(typedBody, StripConfigurationSuffix(owner.GetType().Name) + "Id", owner.Id);

            // Why: WRITE MIRRORS READ. ComposeTypedBody resolves the typed body through
            // ImplementationProviders[ServiceOptionType]; the write resolves it the SAME way, so the discriminator
            // SELECTS the writer and cannot disagree with the body being written. Before this, the write
            // ignored the discriminator entirely and persisted whatever body the caller attached — which is
            // how POST /connections with ServiceType="Http" wrote a conn.MsSqlConnection row under a
            // ServiceOptionType='Http' header (parent said Http, body was MsSql, no HttpConnection row).
            // A registered typed provider OWNS its row and its whole subtree — its own Save cascades them —
            // so hand off rather than walking the mapper here. An empty//unmatched registry means this owner
            // is a leaf or a nested body: the same header-vs-leaf distinction ComposeTypedBody already makes
            // at the read side, not a fallback.
            if (!string.IsNullOrEmpty(owner.ServiceOptionType)
                && ImplementationProviders.TryGetValue(owner.ServiceOptionType, out var typedProvider))
            {
                var delegated = await typedProvider.Save(typedBody, ct).ConfigureAwait(false);
                if (!delegated.IsSuccess) return delegated;
            }
            else
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

    // Why: recursively cascade-save the child-configuration collections declared on <paramref name="owner"/>.
    // Each item is linked to its parent via <paramref name="fkName"/> = <paramref name="fkValue"/> (set here),
    // saved via its per-type ConfigurationCommand, then its OWN child collections are cascaded with the FK
    // re-derived from the item (Strip(item.Type.Name)+"Id" = item.Id). Extracted from CascadeChildSave so the
    // single-vs-N-level walk stays one mechanism and the method stays under FDW007.
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
            // Why: KVP property bags are saved via their own per-entry path — descriptor.ReadDictionary
            // supplies the Name/Value rows, each linked to the owner via fkName=fkValue (same convention
            // as typed-list children). See FDW-547: the prior no-op here silently dropped every KVP
            // property-collection child on save (e.g. conn.MsSqlConnectionAuthentication rows).
            if (descriptors[c].IsPropertyCollection)
            {
                var kvpResult = await SaveKvpChild(owner, descriptors[c], fkName, fkValue, ct).ConfigureAwait(false);
                if (!kvpResult.IsSuccess) return kvpResult;
                continue;
            }
            if (descriptors[c].GetCollection(owner) is not System.Collections.IEnumerable items) continue;
            foreach (var item in items)
            {
                if (item is not IGenericConfiguration childCfg) continue;

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
        // Why: resolve the child's command by ConfigType identity through All(). ConfigurationCommands
        // is keyed by an interface with no name/id member, so the generated ByName/ById are stubs that
        // always return NotFound — matching on ConfigType is the working, convention-free lookup (the
        // same All()-based pattern the provider uses for its own TCommand).
        var command = ConfigurationCommands.All().FirstOrDefault(c => c.ConfigType == childType);
        if (command is null)
        {
            // Why: A non-empty child collection with no registered ConfigurationCommand means the child
            // type was added to the parent's collection but no TypeOption backs it. Fail loud so the
            // omission is caught immediately rather than silently dropping the child row. See CLAUDE.md
            // "NO FALLBACKS WITHOUT EXPLICIT APPROVAL" and "DIAGNOSE AT THE SYSTEM LEVEL, NOT THE SYMPTOM".
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.NoChildCommandForType(
                    _logger, typeof(TConfig).Name, childType.Name));
        }

        if (childCfg.Id == Guid.Empty)
            childCfg.Id = Guid.CreateVersion7();

        // Why: the child shares the parent's DataStore/Path but lives in its own table (ContainerName).
        var saveCmd = command.Create(DataStoreName, PathName, childCfg);
        var childTarget = new DataStoreTarget(DataStoreName, PathName, command.ContainerName);

        // Non-generic IConfigurationGateway.Execute — the child INSERT returns no materialized value and
        // its type is only known at runtime, so it cannot close Execute<T> without reflection.
        var gateway = Gateway();
        if (gateway.IsFailure) return gateway;

        return await gateway.Value!.Execute(saveCmd, childTarget, ct).ConfigureAwait(false);
    }

    // Why: KVP property-collection children (e.g. conn.MsSqlConnectionAuthentication) are not typed
    // configuration collections — the write mirror of LoadKvpChild. descriptor.ReadDictionary supplies
    // the parent's Name/Value bag with NO reflection; each entry is saved as its own KeyValueRow, with
    // the owner FK stamped via AdditionalColumnValues (KeyValueRow has no property for it — the column
    // is physical-only, resolved by MsSqlConfigurationSaveTranslator). A missing ChildContainerName
    // skips exactly like the read side (an unwired bag); a save failure fails loud, never partial-silent.
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

    // Why: the delete mirror of CascadeOwnerChildren, walked in REVERSE. Save persists the owner row and
    // THEN its children; delete must retire the children and THEN the owner, because every child is reached
    // through the owner and the composed read filters children by owner.IsCurrent — retiring the owner first
    // makes its subtree unreachable and leaves it live at rest. Nothing is re-read here: Delete already
    // composed the whole aggregate, so this walks the objects in hand, exactly as the save cascade does.
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
        // Why the REGISTRY decides and not the materialized body: delete composes the header and its
        // children but deliberately not the typed body, so GetTypedBody is null on the root even when a
        // body row exists. A registered typed provider resolves its own row from the OWNER's durable Id —
        // the same key ComposeTypedBody hands it on the read — and retires that row plus everything under
        // it. Passing the body's own id instead would resolve nothing, because a typed-body provider reads
        // Get(Guid) as the PARENT's id.
        if (!string.IsNullOrEmpty(owner.ServiceOptionType)
            && ImplementationProviders.TryGetValue(owner.ServiceOptionType, out var typedProvider))
            return await typedProvider.Delete(owner.Id, ct).ConfigureAwait(false);

        // No registered provider: this owner is a leaf, or a nested body the recursion already
        // materialized — the same distinction the save cascade makes at the same point.
        var typedBody = mapper.GetTypedBody(owner);
        if (typedBody is null)
            return GenericResult.Success();

        var bodyTree = await RetireOwnerChildren(typedBody, ct).ConfigureAwait(false);
        if (!bodyTree.IsSuccess) return bodyTree;

        return await RetireOneChild(typedBody, ct).ConfigureAwait(false);
    }

    // Why: the reverse of CascadeCollections — recurse to the deepest child FIRST, retire on the way back
    // out, so no row is retired before the rows that hang off it. KVP property bags are retired as a scoped
    // set (they have no per-row durable Id — their identity is owner+Name), typed children by their own Id.
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
                if (item is not IGenericConfiguration childCfg) continue;

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
                    _logger, typeof(TConfig).Name, childType.Name));

        var gateway = Gateway();
        if (gateway.IsFailure) return gateway;

        return await gateway.Value!.Execute(
            command.Delete(DataStoreName, PathName, childCfg.Id),
            new DataStoreTarget(DataStoreName, PathName, command.ContainerName),
            ct).ConfigureAwait(false);
    }

    // Why: a KVP property-collection row has no durable Id of its own — its identity is (owner FK, Name),
    // which is exactly the natural key the SAVE translator already versions on. So the retire is scoped by
    // the same logical owner FK the save stamps, and the translator resolves it to the physical
    // {Owner}RowId by subquery. An unwired bag (no child table) skips, identically to the save side.
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
                DefaultConfigurationProviderLog.ConfigurationNotFound(_logger, typeof(TConfig).Name, id.ToString()));

        // Why: DELETE MUST READ FIRST. A cascading soft delete has to retire the aggregate in REVERSE
        // order — deepest child, then typed body, then this header — and once the header row is retired
        // there is no navigation left to reach what belonged to it.
        //
        // Why the header + children compose rather than Get: Get also composes the TYPED BODY, and that
        // step is allowed to fail loud when the discriminator has no registered typed provider
        // delete this" — so a header with an unregistered discriminator became permanently undeletable,
        // and, because exactly one domain overrides that hook, deletable in one domain and not in
        // another for the identical situation. Delete does not need the body materialized: a registered
        // typed provider retires its own row, and an unregistered discriminator is the same leaf case the
        // SAVE cascade already treats as a leaf. Identical machinery for every service type.
        var header = await GetHeaderById(id, null, ct).ConfigureAwait(false);
        if (!header.IsSuccess) return header;
        if (header.Value is null)
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.ConfigurationNotFound(_logger, typeof(TConfig).Name, id.ToString()));

        var existing = await ComposeChildren(header.Value, null, ct).ConfigureAwait(false);
        if (!existing.IsSuccess) return existing;
        if (existing.Value is null)
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.ConfigurationNotFound(_logger, typeof(TConfig).Name, id.ToString()));

        // Why: the exact mirror of the save cascade, walked in REVERSE — child collections first, then the
        // typed body, then (below) this row. Deleting the owner first would strand everything under it,
        // which is how deleting a non-MsSql connection used to orphan its conn.HttpConnection body and how
        // every connection used to leave its auth property-collection rows live under a deleted owner.
        var cascade = await RetireOwnerChildren(existing.Value, ct).ConfigureAwait(false);
        if (!cascade.IsSuccess) return cascade;

        // Why: retire by the row's OWN durable Id, taken from the record we just read — NOT by the incoming
        // `id`. The two are the same for a root table, but on a typed-body provider Get(Guid) resolves by the
        // PARENT's id (see Get(Guid)), so the argument that found the row is not the key that identifies it.
        // Passing the caller's id straight through made the delete command target [Id]=<parent's id> on the
        // child table, which matches nothing and silently retired no row.
        var cmd = Commands().Delete(DataStoreName, PathName, existing.Value.Id);
        var gatewayForDelete = Gateway();
        if (gatewayForDelete.IsFailure) return gatewayForDelete;

        var result = await gatewayForDelete.Value!.Execute<TConfig>(cmd, Target, ct).ConfigureAwait(false);
        if (!result.IsSuccess) return result;

        return GenericResult.Success();
    }

    /// <inheritdoc/>
    // Why: endpoints that identify records by name (e.g. DELETE /glossary/{name}) need a name-based
    // delete path. Resolves the Id via Get(name) first, then delegates to Delete(Guid id) so that
    // cache invalidation and audit columns are handled consistently in one place.
    public virtual async Task<IGenericResult> Delete(string name, CancellationToken ct = default)
    {
        // Why: an absent name and a name that resolves to nothing are both "you asked me to delete
        // something that does not exist" — a caller error, not a no-op. Reporting Success for either
        // told every caller the delete happened when nothing was touched.
        if (string.IsNullOrEmpty(name))
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.ConfigurationNotFound(_logger, typeof(TConfig).Name, name));
        var getResult = await Get(name, ct).ConfigureAwait(false);
        if (!getResult.IsSuccess) return getResult;
        if (getResult.Value is null)
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.ConfigurationNotFound(_logger, typeof(TConfig).Name, name));
        return await Delete(getResult.Value.Id, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Saves a configuration record inside the provided transaction scope.
    /// Use when the save must be atomic with other operations (e.g., security stamp bump).
    /// </summary>
    /// <remarks>
    /// Does NOT cascade child saves and does NOT invalidate the cache — the caller is
    /// responsible for cache invalidation after a successful commit.
    /// </remarks>
    public virtual async Task<IGenericResult<TConfig>> SaveInTransaction(
        TConfig record,
        IDataGatewayTransaction transaction,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.Id == Guid.Empty)
            record.Id = Guid.CreateVersion7();

        // Why: the same single version-on-write path Save uses — the transactional variant differs only in
        // WHERE the command runs, never in what it means to save.
        var result = await transaction.Execute<TConfig>(
            Commands().Create(DataStoreName, PathName, record), Target, ct).ConfigureAwait(false);
        return result.IsSuccess ? GenericResult<TConfig>.Success(record) : result;
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
        // Why: "delete nothing" is a caller error, not a no-op — the same lie Delete(Guid) used to tell.
        // Reporting Success told every caller the delete happened when no row was touched.
        if (id == Guid.Empty)
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.ConfigurationNotFound(_logger, typeof(TConfig).Name, id.ToString()));

        var result = await transaction.Execute<TConfig>(
            Commands().Delete(DataStoreName, PathName, id), Target, ct).ConfigureAwait(false);
        return result.IsSuccess ? GenericResult.Success() : result;
    }

    /// <summary>
    /// Invalidates the cached reads for this provider's table.
    /// Call after committing a transaction whose writes used
    /// <see cref="SaveInTransaction"/> / <see cref="DeleteInTransaction"/>, since those
    /// defer to the caller's transaction and cannot invalidate before commit.
    /// </summary>
    // Why: transactional writes can't invalidate at write time — the rows aren't visible until
    // the caller commits. Without this, a committed role-permission change reads back stale
    // (cached) rows and the grant appears to vanish on reload.
    //
    // Why it asks the gateway instead of holding an invalidator: the gateway owns the cache, so it
    // is the thing that can drop entries. Every non-transactional write is already invalidated by
    // the gateway when the command runs, which is why this is the only invalidation a provider
    // still initiates - and why the provider no longer takes an ICacheInvalidator at all.
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
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The gateway's result, or the failure naming the connection no gateway serves.</returns>
    /// <remarks>
    /// Every provider reaches its store the same way, so resolving the gateway and failing when none
    /// serves the connection belongs here once rather than at each call site.
    /// </remarks>
    protected async Task<IGenericResult<T>> Execute<T>(
        DataGatewayCall call, CancellationToken cancellationToken = default)
    {
        var gateway = Gateway();
        return gateway.IsFailure
            ? gateway.ToNewResult<T>()
            : await gateway.Value!.Execute<T>(call, cancellationToken).ConfigureAwait(false);
    }

}
