using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for DataStoreLoader operations.
/// EventId range: 5185-5199 (loader) plus 5237 (runtime node navigation).
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataStoreLoaderLog
{
    /// <summary>
    /// Logs that a DataStoreLoader LoadAll operation has started.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11119, Level = LogLevel.Trace,
        Message = "[DataStoreLoader] LoadAll started")]
    public static partial IGenericMessage LoaderStarted(ILogger logger);

    /// <summary>
    /// Logs that a DataStoreLoader LoadAll operation completed, reporting the number of stores, paths, and containers loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="storeCount">The number of data stores loaded.</param>
    /// <param name="pathCount">The number of data paths loaded.</param>
    /// <param name="containerCount">The number of data containers loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11120, Level = LogLevel.Information,
        Message = "[DataStoreLoader] LoadAll completed: {storeCount} stores, {pathCount} paths, {containerCount} containers")]
    public static partial IGenericMessage LoaderCompleted(ILogger logger, int storeCount, int pathCount, int containerCount);

    /// <summary>
    /// Logs that a connection type is neither MsSql nor PostgreSql, so a generic DataContainer is used for the named container.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="connectionType">The connection type that was not recognized.</param>
    /// <param name="containerName">The name of the container receiving the generic DataContainer.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11121, Level = LogLevel.Information,
        Message = "[DataStoreLoader] Connection type '{connectionType}' is not MsSql or PostgreSql — using generic DataContainer for '{containerName}'")]
    public static partial IGenericMessage ConnectionTypeUnknown(ILogger logger, string connectionType, string containerName);

    /// <summary>
    /// Logs that a native type on a field could not be resolved in MsSqlNativeTypes, so the NotFound sentinel is used.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="nativeType">The native type that could not be resolved.</param>
    /// <param name="fieldName">The name of the field carrying the unresolved native type.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11122, Level = LogLevel.Information,
        Message = "[DataStoreLoader] NativeType '{nativeType}' on field '{fieldName}' could not be resolved in MsSqlNativeTypes — using NotFound sentinel")]
    public static partial IGenericMessage NativeTypeUnresolved(ILogger logger, string nativeType, string fieldName);

    /// <summary>
    /// Logs that a container with the given RowId was not found in the built tree, so the FK reference will be null.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="containerRowId">The RowId of the container that could not be located in the tree.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    // Why Debug, not Warning: a tree navigation miss is routine caller-handled control flow — the
    // caller treats the absent container as "FK reference will be null" and continues. Re-leveled
    // (EventId 5189 retained) so it sits at the same altitude as the other navigation misses below.
    [MessageLogging(EventId = 11123, Level = LogLevel.Debug,
        Message = "[DataStoreLoader] Container with RowId '{containerRowId}' not found in the built tree — FK reference will be null")]
    public static partial IGenericMessage ContainerNotFound(ILogger logger, string containerRowId);

    /// <summary>
    /// Logs that a path was not found in the named DataStore.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="pathName">The name of the path that could not be found.</param>
    /// <param name="storeName">The name of the DataStore that was searched.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    // Why Debug, not Warning: a path navigation miss is routine caller-handled control flow — the
    // navigation API returns a Failure result the caller inspects (e.g. ResolveContainer iterates
    // paths expecting misses). Re-leveled (EventId 5190 retained) to match the other navigation misses.
    [MessageLogging(EventId = 11124, Level = LogLevel.Debug,
        Message = "[DataStoreLoader] Path '{pathName}' not found in DataStore '{storeName}'")]
    public static partial IGenericMessage PathNotFound(ILogger logger, string pathName, string storeName);

    /// <summary>
    /// Logs that an explicitly-addressed path lookup (not a probe loop) could not find the named path.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="pathName">The name of the path that could not be found.</param>
    /// <param name="storeName">The name of the DataStore that was searched.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    // Why Error, not Debug: this is the terminal, explicitly-addressed altitude — reached only from
    // ConfigurationGatewayDataStoreProvider.Get(dataStoreName, pathName, ct), which
    // DataGatewayService.ResolveContainer calls with a caller-specified target.Path. Unlike PathNotFound
    // above (fired inside probe loops that expect most candidates to miss), a miss here IS the final
    // answer for the one path the caller asked for — the operation cannot complete (FDW-583).
    [MessageLogging(EventId = 71050, Level = LogLevel.Error,
        Message = "[DataStoreLoader] Addressed lookup: path '{pathName}' not found in DataStore '{storeName}'")]
    public static partial IGenericMessage PathNotFoundAddressed(ILogger logger, string pathName, string storeName);

    /// <summary>
    /// Logs that the named DataStore was not found.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="storeName">The name of the DataStore that could not be found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    // Why Debug, not Warning: a store navigation miss is routine caller-handled control flow — the
    // navigation API returns a Failure result the caller inspects and handles. Re-leveled
    // (EventId 5191 retained) to match the other navigation misses.
    [MessageLogging(EventId = 11125, Level = LogLevel.Debug,
        Message = "[DataStoreLoader] DataStore '{storeName}' not found")]
    public static partial IGenericMessage StoreNotFound(ILogger logger, string storeName);

    /// <summary>
    /// Logs that a DataStoreLoader load operation failed, including the captured exception and error detail.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused the load to fail.</param>
    /// <param name="error">The error detail describing the failure.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71025, Level = LogLevel.Error,
        Message = "[DataStoreLoader] Load failed: {error}")]
    public static partial IGenericMessage LoadFailed(ILogger logger, Exception exception, string error);

    /// <summary>
    /// Logs that a native type on a PostgreSql field could not be resolved, so the NotFound sentinel is used.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="nativeType">The native type that could not be resolved.</param>
    /// <param name="fieldName">The name of the PostgreSql field carrying the unresolved native type.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11126, Level = LogLevel.Information,
        Message = "[DataStoreLoader] NativeType '{nativeType}' on PostgreSql field '{fieldName}' could not be resolved — using NotFound sentinel")]
    public static partial IGenericMessage PostgreSqlNativeTypeUnresolved(ILogger logger, string nativeType, string fieldName);

    /// <summary>
    /// Logs that a DataStoreLoader load-by-name operation has started for the named store.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="storeName">The name of the DataStore being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11127, Level = LogLevel.Trace,
        Message = "[DataStoreLoader] Load(name='{storeName}') started")]
    public static partial IGenericMessage LoadByNameStarted(ILogger logger, string storeName);

    /// <summary>
    /// Logs that a DataStore ConnectionId has no matching Connection in the provider, so the connection type is unknown for the named store.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="connectionId">The ConnectionId that had no matching Connection in the provider.</param>
    /// <param name="storeName">The name of the DataStore whose connection type is unknown.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31024, Level = LogLevel.Warning,
        Message = "[DataStoreLoader] DataStore ConnectionId {connectionId} has no matching Connection in provider — connection type unknown for '{storeName}'")]
    public static partial IGenericMessage ConnectionNotFound(ILogger logger, string connectionId, string storeName);

    // Why: 5196 was RegisteringSentinels — removed with sentinel elimination.
    // Keeping the EventId gap so existing log archives remain readable.

    /// <summary>
    /// Logs that a KeyType on a key in a container is not recognized, so the key will be skipped.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="keyType">The KeyType that was not recognized.</param>
    /// <param name="keyName">The name of the key carrying the unrecognized KeyType.</param>
    /// <param name="containerName">The name of the container that owns the key.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61013, Level = LogLevel.Warning,
        Message = "[DataStoreLoader] KeyType '{keyType}' on key '{keyName}' in container '{containerName}' is not recognized — key will be skipped")]
    public static partial IGenericMessage KeyTypeUnresolved(ILogger logger, string keyType, string keyName, string containerName);

    // Why: 5198 was ContainerHasNoTypedBodyParent — removed with TypedBodyParent elimination (FDW-479).
    // Keeping the EventId gap so existing log archives remain readable.

    // Why: 5199 repurposed from SentinelBindingNotSupported — that sentinel no longer exists.
    // Navigation misses now return IGenericResult.Failure; this entry carries the reason.
    /// <summary>
    /// Logs that a DataContainer was not found in the named path during navigation.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="containerName">The name of the DataContainer that could not be found.</param>
    /// <param name="pathName">The name of the path that was searched.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    // Why Debug, not Warning: a container navigation miss is routine caller-handled control flow — the
    // navigation API returns a Failure result the caller inspects (ResolveContainer/DataPath.Container
    // iterate paths expecting misses). Re-leveled (EventId 5199 retained) to match the other misses.
    [MessageLogging(EventId = 11128, Level = LogLevel.Debug,
        Message = "[DataStoreLoader] DataContainer '{containerName}' not found in path '{pathName}'")]
    public static partial IGenericMessage ContainerNotFoundInPath(ILogger logger, string containerName, string pathName);

    /// <summary>
    /// Logs that an explicitly-addressed container lookup (not a probe loop) could not find the named
    /// container in the named path.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="containerName">The name of the DataContainer that could not be found.</param>
    /// <param name="pathName">The name of the path that was searched.</param>
    /// <param name="storeName">The name of the DataStore the path belongs to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    // Why Error, not Debug: this is the terminal, explicitly-addressed altitude — reached only from
    // ConfigurationGatewayDataStoreProvider.Get(dataStoreName, pathName, containerName, ct), which
    // DataGatewayService.ResolveContainer calls with the caller's target.Container. Unlike
    // ContainerNotFoundInPath above (fired inside probe loops such as ConfigurationGateway.ResolveContainer's
    // scan of every path, which expects most candidates to miss), a miss here IS the final answer for the
    // one container the caller asked for — the operation cannot complete (FDW-583).
    [MessageLogging(EventId = 71051, Level = LogLevel.Error,
        Message = "[DataStoreLoader] Addressed lookup: DataContainer '{containerName}' not found in path '{pathName}' of DataStore '{storeName}'")]
    public static partial IGenericMessage ContainerNotFoundInPathAddressed(ILogger logger, string containerName, string pathName, string storeName);

    // Why: FieldNotFoundInContainer (was EventId 5237) moved to DataNodeTreeLog (EventId 5933) in
    // Data.Abstractions alongside the DataContainer base — keeping the gap so log archives stay readable.

    // Why: IDataField is a leaf IDataNode — Node(name) always fails because a field has no children.
    /// <summary>
    /// Logs that a field is a leaf node and therefore has no requested child node.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="fieldName">The name of the leaf field.</param>
    /// <param name="childName">The name of the child node that was requested but does not exist.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 41009, Level = LogLevel.Warning,
        Message = "[DataStoreLoader] Field '{fieldName}' is a leaf node and has no child node '{childName}'")]
    public static partial IGenericMessage LeafFieldHasNoChild(ILogger logger, string fieldName, string childName);

    // ---------------------------------------------------------------------
    // Per-transport builder — FK-direct key resolution (Addendum-B, EventId 5239-5245)
    // ---------------------------------------------------------------------

    /// <summary>
    /// Logs that the DataStoreBuilder Configure call received an unexpected configuration type instead of a DataStoreConfiguration.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="actualType">The actual configuration type that was received.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91016, Level = LogLevel.Error,
        Message = "[DataStoreBuilder] Configure received unexpected configuration type '{actualType}' (expected DataStoreConfiguration)")]
    public static partial IGenericMessage BuilderConfigureWrongType(ILogger logger, string actualType);

    /// <summary>
    /// Logs that the DataStoreBuilder Build call was made before Configure(storeConfig) supplied a configuration.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61014, Level = LogLevel.Error,
        Message = "[DataStoreBuilder] Build requires a configuration — Configure(storeConfig) was not called before Build")]
    public static partial IGenericMessage BuilderNotConfigured(ILogger logger);

    /// <summary>
    /// Logs that an FK key names a referenced container that was not found in the store, so the FK reference will be null.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="keyName">The name of the FK key.</param>
    /// <param name="containerName">The name of the container that owns the FK key.</param>
    /// <param name="referencedContainer">The name of the referenced container that was not found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31025, Level = LogLevel.Warning,
        Message = "[DataStoreBuilder] FK key '{keyName}' on container '{containerName}' names referenced container '{referencedContainer}' which was not found in the store — FK reference will be null")]
    public static partial IGenericMessage BuilderReferencedContainerNotFound(ILogger logger, string keyName, string containerName, string referencedContainer);

    /// <summary>
    /// Logs that an FK key names a referenced key on a referenced container that was not found, so the referenced field is unresolved.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="keyName">The name of the FK key.</param>
    /// <param name="containerName">The name of the container that owns the FK key.</param>
    /// <param name="referencedKey">The name of the referenced key that was not found.</param>
    /// <param name="referencedContainer">The name of the referenced container that owns the referenced key.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31026, Level = LogLevel.Warning,
        Message = "[DataStoreBuilder] FK key '{keyName}' on container '{containerName}' names referenced key '{referencedKey}' on container '{referencedContainer}' which was not found — referenced field unresolved")]
    public static partial IGenericMessage BuilderReferencedKeyNotFound(ILogger logger, string keyName, string containerName, string referencedKey, string referencedContainer);

    /// <summary>
    /// Logs that an FK key's referenced key has no key field to bind to, so the referenced field is unresolved.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="keyName">The name of the FK key.</param>
    /// <param name="containerName">The name of the container that owns the FK key.</param>
    /// <param name="referencedKey">The name of the referenced key that has no key field.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 41010, Level = LogLevel.Warning,
        Message = "[DataStoreBuilder] FK key '{keyName}' on container '{containerName}': referenced key '{referencedKey}' has no key field to bind to — referenced field unresolved")]
    public static partial IGenericMessage BuilderReferencedKeyHasNoField(ILogger logger, string keyName, string containerName, string referencedKey);

    /// <summary>
    /// Logs that a key field of a key is not a declared field on the container, so the key field is skipped.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="fieldName">The name of the key field that is not declared on the container.</param>
    /// <param name="keyName">The name of the key that names the field.</param>
    /// <param name="containerName">The name of the container that was searched.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31027, Level = LogLevel.Warning,
        Message = "[DataStoreBuilder] Key field '{fieldName}' of key '{keyName}' on container '{containerName}' is not a declared field on the container — key field skipped")]
    public static partial IGenericMessage BuilderKeyFieldNotFound(ILogger logger, string fieldName, string keyName, string containerName);

    /// <summary>
    /// Logs that a container references a parent path that is not part of the store under construction.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="containerName">The name of the container referencing the unknown parent path.</param>
    /// <param name="pathName">The name of the parent path that is not part of the store.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31028, Level = LogLevel.Error,
        Message = "[DataStoreBuilder] Container '{containerName}' references parent path '{pathName}' that is not part of the store under construction")]
    public static partial IGenericMessage BuilderContainerParentPathUnknown(ILogger logger, string containerName, string pathName);

    // ---------------------------------------------------------------------
    // Builder coverage — tree-assembly entry/exit, decisions, milestone (EventId 5500-5506).
    // Allocated within the Services.Data 5500-5599 domain range (contiguous, sibling of the
    // 5560/5580 cascade/lookup logs that also live in Services.Data).
    // ---------------------------------------------------------------------

    /// <summary>
    /// Traces entry into the central tree assembler (DataStoreBuilderBase.Build) for a named store.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="storeName">The name of the store being assembled.</param>
    /// <param name="pathCount">The number of paths declared in the store configuration.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11129, Level = LogLevel.Trace,
        Message = "[DataStoreBuilder] Build entry: store='{storeName}', paths={pathCount}")]
    public static partial IGenericMessage BuildEntry(ILogger logger, string storeName, int pathCount);

    /// <summary>
    /// Traces exit from the central tree assembler (DataStoreBuilderBase.Build) with the assembled counts.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="storeName">The name of the store that was assembled.</param>
    /// <param name="pathCount">The number of paths assembled into the store.</param>
    /// <param name="containerCount">The number of containers assembled across all paths.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11130, Level = LogLevel.Trace,
        Message = "[DataStoreBuilder] Build exit: store='{storeName}', paths={pathCount}, containers={containerCount}")]
    public static partial IGenericMessage BuildExit(ILogger logger, string storeName, int pathCount, int containerCount);

    /// <summary>
    /// Logs the decision of which container subtype/ContainerType was chosen for a container.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="containerName">The name of the container.</param>
    /// <param name="containerType">The resolved ContainerType (e.g. table, view, endpoint).</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11131, Level = LogLevel.Debug,
        Message = "[DataStoreBuilder] Container '{containerName}' subtype chosen: {containerType}")]
    public static partial IGenericMessage ContainerSubtypeChosen(ILogger logger, string containerName, string containerType);

    /// <summary>
    /// Logs the count of fields built for a container.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="containerName">The name of the container.</param>
    /// <param name="fieldCount">The number of fields built for the container.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11132, Level = LogLevel.Debug,
        Message = "[DataStoreBuilder] Container '{containerName}' built with {fieldCount} field(s)")]
    public static partial IGenericMessage ContainerFieldsBuilt(ILogger logger, string containerName, int fieldCount);

    /// <summary>
    /// Logs the decision that an FK key's referenced field/column was resolved FK-direct.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="keyName">The name of the FK key whose referenced field was resolved.</param>
    /// <param name="containerName">The name of the container that owns the FK key.</param>
    /// <param name="referencedField">The resolved referenced field name.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11133, Level = LogLevel.Debug,
        Message = "[DataStoreBuilder] FK key '{keyName}' on container '{containerName}' resolved referenced field '{referencedField}'")]
    public static partial IGenericMessage FkReferencedFieldResolved(ILogger logger, string keyName, string containerName, string referencedField);

    /// <summary>
    /// Logs the milestone that a store was fully assembled, with its path and container counts.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="storeName">The name of the assembled store.</param>
    /// <param name="pathCount">The number of paths in the assembled store.</param>
    /// <param name="containerCount">The number of containers in the assembled store.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11134, Level = LogLevel.Information,
        Message = "[DataStoreBuilder] Store '{storeName}' built with {pathCount} path(s), {containerCount} container(s)")]
    public static partial IGenericMessage StoreBuilt(ILogger logger, string storeName, int pathCount, int containerCount);

    /// <summary>
    /// Logs that a container's resolved format is not file-addressable (no canonical file extension), so a
    /// FileSystem-store builder cannot compose a file path for it — the build fails loud.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="containerName">The name of the container whose format is not file-addressable.</param>
    /// <param name="formatName">The resolved format name (e.g. "Tabular", or "_Empty" when unresolved).</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71099, Level = LogLevel.Error,
        Message = "[DataStoreBuilder] Container '{containerName}' format '{formatName}' is not file-addressable (no canonical file extension) — a FileSystem store cannot resolve a file path for it")]
    public static partial IGenericMessage FormatNotFileAddressable(ILogger logger, string containerName, string formatName);
}
