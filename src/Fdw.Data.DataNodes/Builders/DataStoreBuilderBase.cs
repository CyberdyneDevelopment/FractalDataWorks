using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Conventions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.DataNodes;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Builders;

/// <summary>
/// Shared base for the per-transport <see cref="IDataStoreBuilder"/>s. Owns the transport-agnostic
/// assembly of the uniform <see cref="IDataStore"/> tree (store → paths → containers → fields → keys)
/// from a nested <see cref="DataStoreConfiguration"/>, including the FK-direct key resolution
/// (Addendum-B). Transport subclasses override only the two genuinely transport-specific steps:
/// <see cref="BuildField"/> and <see cref="BuildContainer"/>.
/// </summary>
/// <remarks>
/// <para>
/// Replaces the three duplicate tree builders (<c>DataStoreTreeBuilder</c>,
/// <c>ConfigurationGateway.BuildFromSchema</c>, <c>DataStoreProvider.BuildCfgTierContainer</c>) and
/// the never-called <c>DataStoreTypeBase.Build</c>. Containers are built complete and synchronous —
/// fields are child nodes set at construction (no async <c>GetFields</c>, no materialization).
/// </para>
/// <para>
/// FK-direct key resolution: a foreign key carries <c>ReferencedContainerName</c> +
/// <c>ReferencedKeyName</c>. The builder follows <c>ReferencedKeyName</c> → the referenced
/// container's key of that name → that key's first key field → its local field, and uses that as
/// the FK's <c>ReferencedField</c>. No hardcoded <c>"Id"</c>, no tree-walking guesser, every step
/// returns <see cref="IGenericResult{T}"/> (never a <c>Try*</c>/bool/nullable).
/// </para>
/// <para>
/// Cross-reference ordering: container nodes are immutable, but a foreign key must carry the
/// referenced container node and a parent must carry its inbound referencing bindings — circular by
/// nature. The builder therefore builds nodes in two waves: wave A builds bare nodes (no keys, no
/// referencing) into a name→node map; wave B builds the final nodes whose keys resolve
/// <c>ReferencedContainer</c> against the wave-A map and whose <c>ReferencingKeys</c> use the
/// wave-B-resolved inbound index. There is no lazy/async on the node — both waves are synchronous.
/// </para>
/// </remarks>
public abstract class DataStoreBuilderBase : IDataStoreBuilder
{
    private readonly ILogger _logger;
    private DataStoreConfiguration? _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreBuilderBase"/> class.
    /// </summary>
    /// <param name="logger">Logger for build diagnostics. Defaults to a null logger.</param>
    protected DataStoreBuilderBase(ILogger? logger = null)
    {
        // Why: NullLogger keeps the builder functional without DI logging — the only sanctioned ?? fallback.
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Gets the logger used by this builder and its subclasses.</summary>
    protected ILogger Logger => _logger;

    /// <inheritdoc />
    public IGenericResult Configure(IGenericConfiguration storeConfig)
    {
        ArgumentNullException.ThrowIfNull(storeConfig);

        if (storeConfig is not DataStoreConfiguration cfg)
        {
            return GenericResult.Failure(
                DataStoreLoaderLog.BuilderConfigureWrongType(_logger, storeConfig.GetType().Name));
        }

        _config = cfg;
        return GenericResult.Success();
    }

    /// <inheritdoc />
    // Why: a true builder has ONE source — the nested DataStoreConfiguration seeded via Configure.
    // The async signature is kept because a transport may fetch field/key metadata here in a later
    // slice; today the assembly is in-memory, so return a completed task (no sync-over-async).
    // The length/complexity is inherent to the multi-wave composite assembly (build fields → wave-A
    // bare nodes → resolve keys FK-direct → referencing index → wave-B final nodes) and is not
    // reducible without hiding the assembly intent; the per-step work is already delegated to
    // BuildFields/ResolveKeys/BuildReferencingIndex and the transport seams BuildField/BuildContainer.
    [ConventionOverride(MaxCyclomaticComplexity = 20, MaxMethodLines = 110)]
    public Task<IGenericResult<IDataStore>> Build(CancellationToken cancellationToken = default)
    {
        if (_config is null)
        {
            return Task.FromResult(GenericResult<IDataStore>.Failure(
                DataStoreLoaderLog.BuilderNotConfigured(_logger)));
        }

        var cfg = _config;
        DataStoreLoaderLog.BuildEntry(_logger, cfg.Name, cfg.Paths.Count);

        // Why: the fail-loud seam for the void-returning BuildContainer step — a transport that cannot
        // build a given configuration (e.g. the FileSystem builder, when a container's format is not
        // file-addressable) rejects it HERE with a non-success result before any node is built.
        var validation = ValidateConfiguration(cfg);
        if (!validation.IsSuccess)
            return Task.FromResult(validation.ToNewResult<IDataStore>());

        // Pass 1: build the field list per container and index the containers by name so the FK
        // resolution can follow ReferencedContainerName → referenced container config + fields.
        var built = new Dictionary<string, BuiltContainer>(StringComparer.Ordinal);
        foreach (var pathCfg in cfg.Paths)
        {
            foreach (var containerCfg in pathCfg.Containers)
                built[containerCfg.Name] = new BuiltContainer(containerCfg, pathCfg, BuildFields(containerCfg));
        }

        // Why: construct the FINAL store FIRST (empty) so every path below can carry a real Store
        // back-reference, then SetPaths to wire its index — the same set-once shape DataPath uses for its
        // containers. IDataNodePath.Store is non-nullable; building paths with `store: null!` left it null on
        // every runtime path and made the not-found helper throw instead of returning a failure result.
        var store = new DataStore(cfg.Name, cfg.ConnectionId, [], cfg.Description, _logger);

        // Wave A: build bare container nodes (no keys, no referencing) so cross-references resolve to
        // a real node instance regardless of declaration order.
        var bareNodesByName = new Dictionary<string, IDataContainer>(StringComparer.Ordinal);
        foreach (var pathCfg in cfg.Paths)
        {
            var pathBackRef = new DataPath(pathCfg.Name, store, [], pathCfg.Description, _logger);
            foreach (var containerCfg in pathCfg.Containers)
            {
                var bc = built[containerCfg.Name];
                bareNodesByName[containerCfg.Name] = BuildContainer(
                    containerCfg, pathBackRef, bc.Fields, [],
                    GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success([]));
            }
        }

        // Pass 2: resolve keys FK-direct against the wave-A nodes (ReferencedContainer = the bare node).
        var keysByContainerName = new Dictionary<string, IReadOnlyList<IContainerKey>>(StringComparer.Ordinal);
        foreach (var bc in built.Values)
            keysByContainerName[bc.Config.Name] = ResolveKeys(bc, built, bareNodesByName);

        // Pass 3: inbound referencing-key index (which keys point AT each container).
        var referencingByContainerName = BuildReferencingIndex(built, bareNodesByName, keysByContainerName);

        // Wave B: build the final nodes carrying keys + referencing, wired under real parent paths.
        // Why: construct the FINAL path FIRST (empty), build every container parented to THAT same path
        // object, then SetContainers to wire its index. Previously each container was parented to a
        // throwaway empty placeholder path while the populated path was a DIFFERENT object, so
        // container.Parent.Container(sibling) (e.g. a typed-body JOIN) always missed. See DataPath.SetContainers.
        var builtPaths = new List<IDataNodePath>(cfg.Paths.Count);
        foreach (var pathCfg in cfg.Paths)
        {
            var path = new DataPath(pathCfg.Name, store, [], pathCfg.Description, _logger);
            var finalContainers = new List<IDataContainer>(pathCfg.Containers.Count);
            foreach (var containerCfg in pathCfg.Containers)
            {
                var bc = built[containerCfg.Name];
                var referencing = referencingByContainerName.TryGetValue(containerCfg.Name, out var r)
                    ? r
                    : GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success([]);
                finalContainers.Add(BuildContainer(
                    containerCfg, path, bc.Fields, keysByContainerName[containerCfg.Name], referencing));
            }
            path.SetContainers(finalContainers);
            builtPaths.Add(path);
        }

        store.SetPaths(builtPaths);

        var containerCount = 0;
        for (var i = 0; i < builtPaths.Count; i++)
            containerCount += builtPaths[i].Containers.Count;

        DataStoreLoaderLog.StoreBuilt(_logger, cfg.Name, builtPaths.Count, containerCount);
        DataStoreLoaderLog.BuildExit(_logger, cfg.Name, builtPaths.Count, containerCount);

        return Task.FromResult(GenericResult<IDataStore>.Success(store));
    }

    // -------------------------------------------------------------------------
    // Transport-specific seams
    // -------------------------------------------------------------------------

    /// <summary>
    /// Validation hook invoked at the start of <see cref="Build"/> with the store configuration in hand.
    /// The base returns success; a transport subclass overrides it to reject a configuration it cannot
    /// build. This is the fail-loud seam for the void-returning <see cref="BuildContainer"/> step (which
    /// cannot itself return a result) — e.g. the FileSystem builder fails here when a container's format
    /// is not file-addressable, rather than composing a bare, extension-less file path.
    /// </summary>
    /// <param name="config">The store configuration seeded via <see cref="Configure"/>.</param>
    /// <returns>Success to proceed with the build; a failure to reject it (Build returns that failure).</returns>
    protected virtual IGenericResult ValidateConfiguration(DataStoreConfiguration config) => GenericResult.Success();

    /// <summary>
    /// Builds one field node for this transport from its configuration.
    /// </summary>
    /// <param name="fieldCfg">The field configuration row.</param>
    /// <returns>The transport-specific <see cref="IDataField"/> child node.</returns>
    protected abstract IDataField BuildField(DataContainerFieldConfiguration fieldCfg);

    /// <summary>
    /// Builds one container node for this transport from its configuration, with its fields, keys,
    /// and referencing keys fully resolved.
    /// </summary>
    /// <param name="containerCfg">The container configuration row.</param>
    /// <param name="parent">The tree-navigation parent path node.</param>
    /// <param name="fields">The already-built field child nodes (ordered by ordinal).</param>
    /// <param name="keys">The already-resolved keys for this container.</param>
    /// <param name="referencingKeys">The inbound FK references to this container.</param>
    /// <returns>The transport-specific <see cref="IDataContainer"/>.</returns>
    protected abstract IDataContainer BuildContainer(
        DataContainerConfiguration containerCfg,
        IDataNodePath parent,
        IReadOnlyList<IDataField> fields,
        IReadOnlyList<IContainerKey> keys,
        IGenericResult<IReadOnlyList<ReferencingKeyBinding>> referencingKeys);

    // -------------------------------------------------------------------------
    // Fields
    // -------------------------------------------------------------------------

    // Why: concrete List<T> return (not IReadOnlyList<T>) per CA1859 — these are private build helpers
    // whose results are only used internally; the concrete type avoids an interface-dispatch penalty.
    private List<IDataField> BuildFields(DataContainerConfiguration containerCfg)
    {
        var ordered = containerCfg.Fields.OrderBy(f => f.Ordinal).ToList();
        var result = new List<IDataField>(ordered.Count);
        foreach (var fieldCfg in ordered)
            result.Add(BuildField(fieldCfg));
        return result;
    }

    // -------------------------------------------------------------------------
    // FK-direct key resolution (Addendum-B)
    // -------------------------------------------------------------------------

    // Why: concrete List<T> return per CA1859 — private build helper, internal use only.
    private List<IContainerKey> ResolveKeys(
        BuiltContainer owner,
        Dictionary<string, BuiltContainer> built,
        Dictionary<string, IDataContainer> bareNodesByName)
    {
        var keyCfgs = owner.Config.Keys;
        if (keyCfgs.Count == 0)
            return [];

        var localFieldsByName = IndexFields(owner.Fields);
        var result = new List<IContainerKey>(keyCfgs.Count);

        foreach (var keyCfg in keyCfgs)
        {
            var keyType = string.IsNullOrEmpty(keyCfg.TypeId)
                ? KeyTypes.NotFound
                : KeyTypes.ByName(keyCfg.TypeId);

            if (ReferenceEquals(keyType, KeyTypes.NotFound))
            {
                DataStoreLoaderLog.KeyTypeUnresolved(_logger, keyCfg.TypeId ?? string.Empty, keyCfg.Name, owner.Config.Name);
                continue;
            }

            IDataContainer? referencedContainerNode = null;
            BuiltContainer? referencedBuilt = null;
            if (!string.IsNullOrEmpty(keyCfg.ReferencedContainerName))
            {
                if (built.TryGetValue(keyCfg.ReferencedContainerName!, out var refBuilt))
                {
                    referencedBuilt = refBuilt;
                    referencedContainerNode = bareNodesByName[keyCfg.ReferencedContainerName!];
                }
                else
                {
                    DataStoreLoaderLog.BuilderReferencedContainerNotFound(
                        _logger, keyCfg.Name, owner.Config.Name, keyCfg.ReferencedContainerName!);
                }
            }

            var keyFields = BuildKeyFields(keyCfg, owner, localFieldsByName, referencedBuilt);

            var isPhysical = string.Equals(keyCfg.TypeId, "Physical", StringComparison.Ordinal)
                          || string.Equals(keyCfg.TypeId, "PrimaryKey", StringComparison.Ordinal);

            result.Add(new ContainerKey(
                keyName: keyCfg.Name,
                description: keyCfg.Description,
                keyType: (KeyTypeBase)keyType,
                isPhysical: isPhysical,
                referencedContainer: referencedContainerNode,
                keyFields: keyFields));
        }

        return result;
    }

    // Why: concrete List<T> return per CA1859 — private build helper, internal use only.
    private List<IContainerKeyField> BuildKeyFields(
        DataContainerKeyConfiguration keyCfg,
        BuiltContainer owner,
        Dictionary<string, IDataField> localFieldsByName,
        BuiltContainer? referencedBuilt)
    {
        if (keyCfg.KeyFields.Count == 0)
            return [];

        // Why (FK-direct): the referenced field is the field bound by the referenced container's key
        // named ReferencedKeyName — follow the FK's direct link. Resolved once per key and shared
        // across all participating key fields. NO hardcoded "Id", NO guesser.
        var referencedField = ResolveReferencedField(keyCfg, owner.Config.Name, referencedBuilt);

        var result = new List<IContainerKeyField>(keyCfg.KeyFields.Count);
        foreach (var kfCfg in keyCfg.KeyFields.OrderBy(kf => kf.Ordinal))
        {
            if (kfCfg.Name is null)
                continue;

            if (!localFieldsByName.TryGetValue(kfCfg.Name, out var localField))
            {
                DataStoreLoaderLog.BuilderKeyFieldNotFound(_logger, kfCfg.Name, keyCfg.Name, owner.Config.Name);
                continue;
            }

            result.Add(new ContainerKeyField(localField, referencedField, kfCfg.Ordinal));
        }

        return result;
    }

    private IDataField? ResolveReferencedField(
        DataContainerKeyConfiguration keyCfg,
        string ownerContainerName,
        BuiltContainer? referencedBuilt)
    {
        if (string.IsNullOrEmpty(keyCfg.ReferencedContainerName) || string.IsNullOrEmpty(keyCfg.ReferencedKeyName))
            return null;

        if (referencedBuilt is null)
            return null; // already logged in ResolveKeys

        var referencedKeyCfg = referencedBuilt.Config.Keys
            .FirstOrDefault(k => string.Equals(k.Name, keyCfg.ReferencedKeyName, StringComparison.Ordinal));

        if (referencedKeyCfg is null)
        {
            DataStoreLoaderLog.BuilderReferencedKeyNotFound(
                _logger, keyCfg.Name, ownerContainerName, keyCfg.ReferencedKeyName!, keyCfg.ReferencedContainerName!);
            return null;
        }

        var referencedFieldName = referencedKeyCfg.KeyFields
            .OrderBy(kf => kf.Ordinal)
            .Select(kf => kf.Name)
            .FirstOrDefault(n => !string.IsNullOrEmpty(n));

        if (referencedFieldName is null)
        {
            DataStoreLoaderLog.BuilderReferencedKeyHasNoField(
                _logger, keyCfg.Name, ownerContainerName, keyCfg.ReferencedKeyName!);
            return null;
        }

        var referencedFieldIndex = IndexFields(referencedBuilt.Fields);
        if (referencedFieldIndex.TryGetValue(referencedFieldName, out var field))
        {
            DataStoreLoaderLog.FkReferencedFieldResolved(_logger, keyCfg.Name, ownerContainerName, referencedFieldName);
            return field;
        }

        return null;
    }

    // -------------------------------------------------------------------------
    // Referencing-key index (inbound FKs per container)
    // -------------------------------------------------------------------------

    private static Dictionary<string, IGenericResult<IReadOnlyList<ReferencingKeyBinding>>> BuildReferencingIndex(
        Dictionary<string, BuiltContainer> built,
        Dictionary<string, IDataContainer> bareNodesByName,
        Dictionary<string, IReadOnlyList<IContainerKey>> keysByContainerName)
    {
        var inbound = new Dictionary<string, List<ReferencingKeyBinding>>(StringComparer.Ordinal);
        foreach (var name in built.Keys)
            inbound[name] = [];

        foreach (var (childName, keys) in keysByContainerName)
        {
            var childNode = bareNodesByName[childName];
            foreach (var key in keys)
            {
                var referenced = key.ReferencedContainer;
                if (referenced is null)
                    continue;

                if (inbound.TryGetValue(referenced.Name, out var bindings))
                    bindings.Add(new ReferencingKeyBinding(key, childNode));
            }
        }

        var result = new Dictionary<string, IGenericResult<IReadOnlyList<ReferencingKeyBinding>>>(StringComparer.Ordinal);
        foreach (var (name, bindings) in inbound)
            result[name] = GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success(bindings);
        return result;
    }

    private static Dictionary<string, IDataField> IndexFields(IReadOnlyList<IDataField> fields)
    {
        var index = new Dictionary<string, IDataField>(fields.Count, StringComparer.Ordinal);
        foreach (var f in fields)
            index[f.Name] = f;
        return index;
    }

    // Why: carries the config + fields through the multi-pass build so FK resolution can read the
    // referenced container's config (for ReferencedKeyName) and fields (to bind the referenced field).
    private sealed class BuiltContainer(
        DataContainerConfiguration config,
        DataPathConfiguration path,
        IReadOnlyList<IDataField> fields)
    {
        public DataContainerConfiguration Config { get; } = config;
        public DataPathConfiguration Path { get; } = path;
        public IReadOnlyList<IDataField> Fields { get; } = fields;
    }
}
