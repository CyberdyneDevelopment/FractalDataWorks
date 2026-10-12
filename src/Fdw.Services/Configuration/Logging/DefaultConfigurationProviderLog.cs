using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

using Fdw.Services.Data.Abstractions;
namespace Fdw.Services.Configuration.Logging;

/// <summary>
/// MessageLogging for the configuration providers' operations.
/// </summary>
/// <remarks>
/// EventId range: 11000-11018, 11039-11041, 20000, 30000, 31000-31003, 41000, 61000-61013,
/// 71000-71004, 91000-91003. The 9360-9388 range this file's own comment previously claimed does
/// not match any EventId actually declared here -- stale even before this addition.
/// </remarks>
[MessageLoggingTypeCode("SERVICES")]
public static partial class DefaultConfigurationProviderLog
{
    // ── Trace (9360-9362) ──

    /// <summary>
    /// Logs the result of a system-scope lookup for a configuration type by name.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The configuration type name being looked up.</param>
    /// <param name="name">The name of the configuration being looked up.</param>
    /// <param name="result">The outcome of the system lookup.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "System lookup for {typeName} '{name}': {result}")]
    public static partial IGenericMessage SystemLookup(ILogger logger, string typeName, string name, string result);

    /// <summary>
    /// Logs the result of a user-cache lookup for a configuration type by name.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The configuration type name being looked up.</param>
    /// <param name="name">The name of the configuration being looked up.</param>
    /// <param name="result">The outcome of the user-cache lookup.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "User cache lookup for {typeName} '{name}': {result}")]
    public static partial IGenericMessage UserCacheLookup(ILogger logger, string typeName, string name, string result);

    // ── DomainConfigurationProviderBase read/write (11014-11020) ──
    // Get/Find/Save/Delete had no logging at all on their successful path -- a miss was
    // indistinguishable from a hit, since neither logged anything. This is the shared machinery
    // every domain provider (RoleConfigurationProvider, PermissionConfigurationProvider, etc.)
    // reads and writes through, so its silence was every domain's silence.

    /// <summary>No domain row exists under the key a read was asked for.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The implementation contract this provider composes.</param>
    /// <param name="key">The name or id that was looked up.</param>
    [MessageLogging(EventId = 11014, Level = LogLevel.Trace,
        Message = "No {typeName} domain row found for '{key}'")]
    public static partial IGenericMessage DomainRowNotFound(ILogger logger, string typeName, string key);

    /// <summary>A domain row was found and its implementation composed.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The implementation contract this provider composes.</param>
    /// <param name="name">The domain row's name.</param>
    /// <param name="id">The domain row's logical id.</param>
    /// <param name="implementation">The Implementation the row named.</param>
    [MessageLogging(EventId = 11015, Level = LogLevel.Trace,
        Message = "Composed {typeName} '{name}' ({id}) from Implementation '{implementation}'")]
    public static partial IGenericMessage DomainRowComposed(
        ILogger logger, string typeName, string name, Guid id, string implementation);

    /// <summary>A bulk read composed every current, non-deleted row for the domain.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The implementation contract this provider composes.</param>
    /// <param name="rowCount">How many domain rows were read.</param>
    /// <param name="composedCount">How many composed successfully (may be fewer, if a row named an
    /// implementation with no value to return -- see <see cref="ImplementationProviderBase{TConfiguration,TContract}"/>).</param>
    [MessageLogging(EventId = 11016, Level = LogLevel.Trace,
        Message = "Composed {composedCount} of {rowCount} {typeName} row(s)")]
    public static partial IGenericMessage DomainListComposed(
        ILogger logger, string typeName, int rowCount, int composedCount);

    /// <summary>A domain row and its implementation were written.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The implementation contract this provider composes.</param>
    /// <param name="name">The name it was saved under.</param>
    /// <param name="id">The domain row's logical id -- newly minted if this was the first save.</param>
    /// <param name="domainRowAction">Whether the domain row was minted new or an existing one reused.</param>
    [MessageLogging(EventId = 11017, Level = LogLevel.Information,
        Message = "Saved {typeName} '{name}' ({id}) -- domain row {domainRowAction}")]
    public static partial IGenericMessage DomainRowSaved(
        ILogger logger, string typeName, string name, Guid id, string domainRowAction);

    /// <summary>A domain row and its implementation were deleted.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The implementation contract this provider composes.</param>
    /// <param name="id">The domain row's logical id.</param>
    [MessageLogging(EventId = 11018, Level = LogLevel.Information,
        Message = "Deleted {typeName} {id}")]
    public static partial IGenericMessage DomainRowDeleted(ILogger logger, string typeName, Guid id);

    // ── ImplementationProviderBase read/write (11019-11023) ──
    // Same gap, one layer down: Get/Save/Delete here had no logging on the successful path either.
    // This is the layer that actually reads the typed body (MsSqlConnection, RoleImplementation,
    // etc.) and composes its child collections.

    /// <summary>No typed implementation row exists for a domain id.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The configuration type being read.</param>
    /// <param name="domainId">The owning domain row's id.</param>
    [MessageLogging(EventId = 11019, Level = LogLevel.Trace,
        Message = "No {typeName} implementation row found for domain id {domainId}")]
    public static partial IGenericMessage ImplementationNotFound(ILogger logger, string typeName, Guid domainId);

    /// <summary>A typed implementation row was found and composed.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The configuration type read.</param>
    /// <param name="domainId">The owning domain row's id.</param>
    [MessageLogging(EventId = 11020, Level = LogLevel.Trace,
        Message = "Composed {typeName} for domain id {domainId}")]
    public static partial IGenericMessage ImplementationComposed(ILogger logger, string typeName, Guid domainId);

    /// <summary>A bulk read composed every current, non-deleted implementation row.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The configuration type read.</param>
    /// <param name="count">How many composed.</param>
    [MessageLogging(EventId = 11021, Level = LogLevel.Trace,
        Message = "Composed {count} {typeName} row(s)")]
    public static partial IGenericMessage ImplementationListComposed(ILogger logger, string typeName, int count);

    /// <summary>A typed implementation row and its child collections were written.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The configuration type saved.</param>
    /// <param name="id">The row's id.</param>
    [MessageLogging(EventId = 11022, Level = LogLevel.Information,
        Message = "Saved {typeName} {id}")]
    public static partial IGenericMessage ImplementationSaved(ILogger logger, string typeName, Guid id);

    /// <summary>A typed implementation row and its child collections were deleted.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The configuration type deleted.</param>
    /// <param name="domainId">The owning domain row's id.</param>
    [MessageLogging(EventId = 11023, Level = LogLevel.Information,
        Message = "Deleted {typeName} for domain id {domainId}")]
    public static partial IGenericMessage ImplementationDeleted(ILogger logger, string typeName, Guid domainId);

    /// <summary>
    /// Logs that the gateway is being resolved for a configuration type via the lazy data gateway.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The configuration type name the gateway is being resolved for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "Gateway resolving for {typeName} via IDataGatewayProvider")]
    public static partial IGenericMessage GatewayResolving(ILogger logger, string typeName);

    // ── Debug (9363) ──

    /// <summary>
    /// Logs the system and user counts being returned by a get-all operation for a configuration type.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="systemCount">The number of system-scope configurations being returned.</param>
    /// <param name="userCount">The number of user-scope configurations being returned.</param>
    /// <param name="typeName">The configuration type name being returned.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug,
        Message = "GetAll returning {systemCount} system + {userCount} user {typeName}")]
    public static partial IGenericMessage GetAllCounts(ILogger logger, int systemCount, int userCount, string typeName);

    // ── Information (9364) ──

    /// <summary>
    /// Logs that the system index was rebuilt for a configuration type, reporting the entry count.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The configuration type name whose system index was rebuilt.</param>
    /// <param name="count">The number of entries in the rebuilt system index.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information,
        Message = "System index rebuilt for {typeName}: {count} entries")]
    public static partial IGenericMessage SystemIndexRebuilt(ILogger logger, string typeName, int count);

    // ── Warning (9365-9367) ──

    /// <summary>
    /// Logs that a name collision occurred for a configuration type and that the system entry takes precedence.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The configuration type name that collided.</param>
    /// <param name="name">The colliding configuration name.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 41000, Level = LogLevel.Warning,
        Message = "Name collision for {typeName} '{name}' -- system takes precedence")]
    public static partial IGenericMessage NameCollision(ILogger logger, string typeName, string name);

    /// <summary>
    /// Logs that the gateway is unavailable for a configuration type and that only system-scope configuration is returned.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The configuration type name the gateway was unavailable for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71000, Level = LogLevel.Warning,
        Message = "Gateway unavailable for {typeName} -- returning system-only config")]
    public static partial IGenericMessage GatewayUnavailable(ILogger logger, string typeName);

    // ── Error (9368-9372) ──

    /// <summary>
    /// Logs that a data gateway query for a configuration type by name failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The configuration type name being queried.</param>
    /// <param name="name">The name of the configuration being queried.</param>
    /// <param name="error">The error message describing the query failure.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "DataGateway query failed for {typeName} '{name}': {error}")]
    public static partial IGenericMessage QueryByNameFailed(ILogger logger, string typeName, string name, string error);

    /// <summary>
    /// Logs that a data gateway get-all query for a configuration type failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The configuration type name being queried.</param>
    /// <param name="error">The error message describing the query failure.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error,
        Message = "DataGateway query failed for {typeName} GetAll: {error}")]
    public static partial IGenericMessage QueryAllFailed(ILogger logger, string typeName, string error);

    /// <summary>
    /// Logs that a data gateway query for a configuration type by identifier failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The configuration type name being queried.</param>
    /// <param name="id">The identifier of the configuration being queried.</param>
    /// <param name="error">The error message describing the query failure.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71003, Level = LogLevel.Error,
        Message = "DataGateway query failed for {typeName} by ID '{id}': {error}")]
    public static partial IGenericMessage QueryByIdFailed(ILogger logger, string typeName, string id, string error);

    /// <summary>
    /// Logs that resolving the lazy data gateway for a configuration type failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The configuration type name the gateway resolution failed for.</param>
    /// <param name="error">The error message describing the resolution failure.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61000, Level = LogLevel.Error,
        Message = "Data gateway could not be supplied for {typeName}: {error}")]
    public static partial IGenericMessage GatewayResolutionFailed(ILogger logger, string typeName, string error);

    /// <summary>
    /// Logs an unexpected error that occurred in a specific method of a configuration type.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The configuration type name where the error occurred.</param>
    /// <param name="methodName">The name of the method where the error occurred.</param>
    /// <param name="error">The error message describing the unexpected failure.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error,
        Message = "Unexpected error in {typeName}.{methodName}: {error}")]
    public static partial IGenericMessage UnexpectedError(ILogger logger, string typeName, string methodName, string error);

    // ── Warning (9373) ──

    /// <summary>
    /// Logs that cache invalidation failed for a tag in a configuration type even though the write succeeded.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="tag">The cache tag that failed to be invalidated.</param>
    /// <param name="typeName">The configuration type name whose cache invalidation failed.</param>
    /// <param name="error">The error message describing the invalidation failure.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91001, Level = LogLevel.Warning,
        Message = "Cache invalidation failed for tag '{tag}' in {typeName} (write succeeded): {error}")]
    public static partial IGenericMessage CacheInvalidationFailed(ILogger logger, string tag, string typeName, string error);

    // ── Warning (9374-9375) ── IDataStore tree key resolution

    /// <summary>
    /// Logs that a container was not found in a data store for a configuration type. The caller fails; nothing is
    /// read in its place.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="configTypeName">The configuration type name being resolved.</param>
    /// <param name="containerName">The name of the container that was not found.</param>
    /// <param name="dataStoreName">The name of the data store the container was expected in.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning,
        Message = "Container '{containerName}' not found in store '{dataStoreName}' for {configTypeName}")]
    public static partial IGenericMessage ContainerNotFoundInStore(ILogger logger, string configTypeName, string containerName, string dataStoreName);

    /// <summary>
    /// Logs that no suitable key was found for a container. The caller fails; nothing is read in its place.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="configTypeName">The configuration type name being resolved.</param>
    /// <param name="containerName">The name of the container that has no suitable key.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31001, Level = LogLevel.Warning,
        Message = "No suitable key found for container '{containerName}' in {configTypeName}")]
    public static partial IGenericMessage NoSuitableKeyForContainer(ILogger logger, string configTypeName, string containerName);

    /// <summary>
    /// Logged when an owner row carries no value under its container's declared Logical key, so its
    /// children cannot be loaded.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="configTypeName">The configuration type name being loaded.</param>
    /// <param name="containerName">The owner's container.</param>
    /// <param name="logicalKey">The Logical key column the container declares.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31004, Level = LogLevel.Warning,
        Message = "Owner '{configTypeName}' in container '{containerName}' has no '{logicalKey}' value — its children were not loaded")]
    public static partial IGenericMessage OwnerLogicalKeyMissing(ILogger logger, string configTypeName, string containerName, string logicalKey);

    /// <summary>
    /// Logged when an owner row's declared Logical key is present but empty, which cannot address
    /// any child rows.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="configTypeName">The configuration type name being loaded.</param>
    /// <param name="containerName">The owner's container.</param>
    /// <param name="logicalKey">The Logical key column the container declares.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31005, Level = LogLevel.Warning,
        Message = "Owner '{configTypeName}' in container '{containerName}' has an empty '{logicalKey}' — its children were not loaded")]
    public static partial IGenericMessage OwnerLogicalKeyEmpty(ILogger logger, string configTypeName, string containerName, string logicalKey);

    /// <summary>
    /// Logged when the gateway carries no data store by the provider's name, so no container in it can
    /// be looked up.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="containerName">The container whose keys were being looked up.</param>
    /// <param name="dataStoreName">The data store the provider reads from.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31006, Level = LogLevel.Warning,
        Message = "No data store '{dataStoreName}' on the gateway -- cannot look up keys for container '{containerName}'")]
    public static partial IGenericMessage DataStoreNotFound(ILogger logger, string containerName, string dataStoreName);

    /// <summary>
    /// Logged when a data store answers a path lookup successfully but with nothing in it.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="containerName">The container whose keys were being looked up.</param>
    /// <param name="pathName">The path the container was expected under.</param>
    /// <param name="dataStoreName">The data store the path was looked up in.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31007, Level = LogLevel.Warning,
        Message = "Path '{pathName}' not found in store '{dataStoreName}' -- cannot look up keys for container '{containerName}'")]
    public static partial IGenericMessage PathNotFound(ILogger logger, string containerName, string pathName, string dataStoreName);

    /// <summary>
    /// Logged when a container is found but declares no key of the type a lookup needs. Names the key
    /// type, so a missing Physical key and a missing Logical key read differently.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="containerName">The container that declares no such key.</param>
    /// <param name="keyTypeName">The key type that was not declared -- Physical or Logical.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31008, Level = LogLevel.Warning,
        Message = "Container '{containerName}' declares no {keyTypeName} key")]
    public static partial IGenericMessage ContainerKeyNotDeclared(ILogger logger, string containerName, string keyTypeName);

    // ── Error (9376) ──

    /// <summary>
    /// Logs that building the filter expression for a container in a configuration type failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="configTypeName">The configuration type name being resolved.</param>
    /// <param name="containerName">The name of the container whose filter expression build failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error,
        Message = "Filter expression build failed for container '{containerName}' in {configTypeName}")]
    public static partial IGenericMessage FilterExpressionBuildFailed(ILogger logger, string configTypeName, string containerName);

    // ── Trace (9377-9378) — child-only container name resolution via parent ──

    /// <summary>
    /// Logs that a child-only container lookup is resolving its parent container by name before fetching the child.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="configTypeName">The configuration type name being resolved.</param>
    /// <param name="containerName">The name of the child-only container being resolved.</param>
    /// <param name="parentContainerName">The name of the parent container being resolved.</param>
    /// <param name="name">The name used to resolve the parent container.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace,
        Message = "Child-only container '{containerName}' — resolving parent '{parentContainerName}' by name '{name}' for {configTypeName}")]
    public static partial IGenericMessage ChildOnlyResolvingParent(ILogger logger, string configTypeName, string containerName, string parentContainerName, string name);

    /// <summary>
    /// Logs that a parent container was resolved to an identifier and that the child container is being fetched by foreign key.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="configTypeName">The configuration type name being resolved.</param>
    /// <param name="containerName">The name of the child container being fetched.</param>
    /// <param name="parentContainerName">The name of the resolved parent container.</param>
    /// <param name="domainConfigurationId">The resolved identifier of the parent container.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11006, Level = LogLevel.Trace,
        Message = "Parent '{parentContainerName}' resolved id='{domainConfigurationId}' — fetching child '{containerName}' by FK for {configTypeName}")]
    public static partial IGenericMessage ChildOnlyParentResolved(ILogger logger, string configTypeName, string containerName, string parentContainerName, string domainConfigurationId);

    // ── Error (9379) — explicit key overload ──

    /// <summary>
    /// Logs that a name-based get was called on a container that has no natural key and incomplete foreign-key metadata, so an identifier-based or explicit-key lookup must be used instead.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="configTypeName">The configuration type name being resolved.</param>
    /// <param name="containerName">The name of the container that has no natural key.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31002, Level = LogLevel.Error,
        Message = "Get(string name) called on container '{containerName}' for {configTypeName} but no Natural key is defined and FK metadata is incomplete — use Get(Guid id) or Get(IContainerKey, value)")]
    public static partial IGenericMessage NoNaturalKeyForContainer(ILogger logger, string configTypeName, string containerName);

    // ── Error (9350) — missing ConfigurationCommand for non-empty child ──

    /// <summary>
    /// Logs that a cascade child save found no configuration command registered for a child type on a parent type, so a type option must be added or the child removed from the parent's collection property.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="parentTypeName">The parent configuration type name being saved.</param>
    /// <param name="childTypeName">The child configuration type name that has no registered command.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61001, Level = LogLevel.Error,
        Message = "CascadeChildSave: no ConfigurationCommand registered for child type '{childTypeName}' on parent '{parentTypeName}' — add a [TypeOption] for this child or remove it from the parent's IEnumerable property")]
    public static partial IGenericMessage NoChildCommandForType(ILogger logger, string parentTypeName, string childTypeName);

    /// <summary>
    /// Logs that a cascade skipped a child collection item because it does not implement IGenericConfiguration, so the row was never written and no error was raised.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="parentTypeName">The parent configuration type name being saved.</param>
    /// <param name="childTypeName">The child type that was skipped.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61002, Level = LogLevel.Error,
        Message = "CascadeChildSave: skipped '{childTypeName}' on parent '{parentTypeName}' because it does not implement IGenericConfiguration — the row was NOT written and nothing else will report this; add the interface to the child configuration")]
    public static partial IGenericMessage ChildSkippedNotConfiguration(ILogger logger, string parentTypeName, string childTypeName);

    // ── Typed-body composition (9380-9385) — the read mirror of the typed-body save ──

    /// <summary>
    /// Logs that a typed-body configuration provider was registered for a discriminator on a header type.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The header configuration type name the typed provider was registered on.</param>
    /// <param name="implementation">The discriminator the typed provider was registered for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11007, Level = LogLevel.Trace,
        Message = "Registered typed-body provider for {typeName} discriminator '{implementation}'")]
    public static partial IGenericMessage TypedProviderRegistered(ILogger logger, string typeName, string implementation);

    /// <summary>
    /// Logs that the typed body is being loaded for a header using its discriminator.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The header configuration type name whose typed body is being loaded.</param>
    /// <param name="name">The name of the header whose typed body is being loaded.</param>
    /// <param name="implementation">The discriminator used to load the typed body.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11008, Level = LogLevel.Trace,
        Message = "Loading typed body for {typeName} '{name}' using discriminator '{implementation}'")]
    public static partial IGenericMessage LoadingTypedBody(ILogger logger, string typeName, string name, string implementation);

    /// <summary>
    /// Logs that the typed body was successfully loaded and attached to a header.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The header configuration type name whose typed body was loaded.</param>
    /// <param name="name">The name of the header whose typed body was loaded.</param>
    /// <param name="implementation">The discriminator used to load the typed body.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11009, Level = LogLevel.Trace,
        Message = "Typed body loaded for {typeName} '{name}' (discriminator '{implementation}')")]
    public static partial IGenericMessage TypedBodyLoaded(ILogger logger, string typeName, string name, string implementation);

    /// <summary>
    /// Logs that a header carries no discriminator, so its typed body cannot be composed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The header configuration type name missing a discriminator.</param>
    /// <param name="name">The name of the header missing a discriminator.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11010, Level = LogLevel.Debug,
        Message = "Header {typeName} '{name}' has no Implementation — typed body not composed")]
    public static partial IGenericMessage NoImplementationForTypedBody(ILogger logger, string typeName, string name);

    /// <summary>Logs that no implementation configuration provider is registered for a Implementation.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The configuration's name.</param>
    /// <param name="implementation">The Implementation the record names.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61006, Level = LogLevel.Error,
        Message = "No implementation configuration provider registered for Implementation '{implementation}' — cannot compose '{name}'")]
    public static partial IGenericMessage NoImplementationProvider(ILogger logger, string name, string implementation);

    /// <summary>
    /// Logged when a provider offered for registration cannot be erased to the interface the
    /// registry stores.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The name the provider was being registered under.</param>
    /// <param name="providerType">The type that was offered.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    /// <remarks>
    /// Distinct from <see cref="NoImplementationProvider"/> because nothing was looked up: this is a
    /// failed cast at registration time, and the Implementation may be registered perfectly well.
    /// Sharing one message sent readers to check a registry that was not the problem.
    /// </remarks>
    [MessageLogging(EventId = 61007, Level = LogLevel.Error,
        Message = "Provider offered for '{name}' is not an IServiceConfigurationProvider — it is '{providerType}', so it cannot be registered")]
    public static partial IGenericMessage ProviderNotErasable(ILogger logger, string name, string providerType);

    /// <summary>
    /// Logged when a composed domain record carries no Implementation to dispatch on.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="identifier">The name or id the record was read for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    /// <remarks>
    /// Also distinct: nothing was looked up here either, because there was nothing to look up with.
    /// The row itself is incomplete, which is a data problem rather than a registration one.
    /// </remarks>
    [MessageLogging(EventId = 61008, Level = LogLevel.Error,
        Message = "Domain record '{identifier}' carries no Implementation, so no implementation provider can be chosen for it")]
    public static partial IGenericMessage RecordHasNoImplementation(ILogger logger, string identifier);

    /// <summary>
    /// Logged when a domain record names a registered implementation but carries none.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="identifier">The record's name.</param>
    /// <param name="implementation">The implementation the record names.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    /// <remarks>
    /// The write half of the same fault <see cref="NoImplementationProvider"/> names on read. A row
    /// that names an implementation and carries none is the bodiless domain record that cannot be
    /// composed later, so it is refused here rather than written and failed at boot.
    /// </remarks>
    [MessageLogging(EventId = 61010, Level = LogLevel.Error,
        Message = "Domain record '{identifier}' names implementation '{implementation}' but carries no implementation configuration, so it would be saved as a record nothing can compose")]
    public static partial IGenericMessage IncompleteAggregate(ILogger logger, string identifier, string implementation);

    /// <summary>
    /// Logs that no POCO mapper was found for a header type, so the loaded typed body was left unattached.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The header configuration type name with no registered mapper.</param>
    /// <param name="name">The name of the header whose typed body was left unattached.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11011, Level = LogLevel.Debug,
        Message = "No PocoMapper for {typeName} '{name}' — typed body loaded but not attached")]
    public static partial IGenericMessage NoMapperForTypedBody(ILogger logger, string typeName, string name);

    /// <summary>
    /// Logs that no typed-body provider is registered for a header's discriminator.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The header configuration type name being composed.</param>
    /// <param name="implementation">The discriminator for which no typed provider was found.</param>
    /// <param name="name">The name of the header that could not be composed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61002, Level = LogLevel.Error,
        Message = "No typed-body provider registered for {typeName} discriminator '{implementation}' (header '{name}')")]
    public static partial IGenericMessage NoTypedProviderForImplementation(ILogger logger, string typeName, string implementation, string name);

    /// <summary>
    /// Logs that the configuration record a caller asked to act on does not exist.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="typeName">The configuration type being resolved.</param>
    /// <param name="identifier">The name or Id that resolved to nothing.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 30000, Level = LogLevel.Error,
        Message = "{typeName} '{identifier}' was not found")]
    public static partial IGenericMessage ConfigurationNotFound(ILogger logger, string typeName, string identifier);

    /// <summary>
    /// Logs that a polymorphic header was saved without the typed body its discriminator requires.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="typeName">The header configuration type being saved.</param>
    /// <param name="name">The name of the record being saved.</param>
    /// <param name="implementation">The discriminator whose typed provider is registered.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 20000, Level = LogLevel.Error,
        Message = "{typeName} '{name}' declares '{implementation}' but carries no typed body; the aggregate is incomplete and cannot be saved")]
    public static partial IGenericMessage TypedBodyMissingOnSave(ILogger logger, string typeName, string name, string implementation);

    /// <summary>
    /// Logs that loading the typed body failed for a header.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="exception">The exception that caused the typed body load to fail.</param>
    /// <param name="typeName">The header configuration type name whose typed body failed to load.</param>
    /// <param name="name">The name of the header whose typed body failed to load.</param>
    /// <param name="implementation">The discriminator used when the load failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71004, Level = LogLevel.Error,
        Message = "Failed to load typed body for {typeName} '{name}' (discriminator '{implementation}')")]
    public static partial IGenericMessage TypedBodyLoadFailed(ILogger logger, Exception exception, string typeName, string name, string implementation);

    // ── Child composition (9387) — the read mirror of the child-collection save ──

    /// <summary>
    /// Logs that an inbound FK binding has no matching child descriptor on the owner mapper, so it is skipped.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="keyName">The FK key name of the skipped inbound binding.</param>
    /// <param name="childContainerName">The child container that declares the inbound FK.</param>
    /// <param name="ownerTypeName">The owner configuration type name being composed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11012, Level = LogLevel.Debug,
        Message = "Child composition: FK key '{keyName}' from child container '{childContainerName}' has no descriptor on {ownerTypeName} — skipped (typed-body or cross-cutting FK)")]
    public static partial IGenericMessage ChildBindingSkippedNoDescriptor(ILogger logger, string keyName, string childContainerName, string ownerTypeName);

    // ── Trace (9389) — KVP property-collection child save (FDW-547) ──

    /// <summary>
    /// Logs that a property-collection (KVP) child bag was cascade-saved for an owner, reporting the
    /// entry count.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="ownerTypeName">The owner configuration type name whose KVP bag was saved.</param>
    /// <param name="childContainerName">The KVP child container the entries were saved to.</param>
    /// <param name="count">The number of key/value entries saved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11013, Level = LogLevel.Trace,
        Message = "KVP child '{childContainerName}' saved {count} entries for owner {ownerTypeName}")]
    public static partial IGenericMessage KvpChildSaved(ILogger logger, string ownerTypeName, string childContainerName, int count);

    // ── Error (9388) — implementation name lookup guard ──

    /// <summary>
    /// Logs that a name-based lookup was called on an implementation table that has a domain
    /// parent join and therefore no Name column. The caller must resolve by parent Id (Get(Guid)) instead.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The configuration type name that cannot be resolved by name.</param>
    /// <param name="tableName">The table name that has no Name column.</param>
    /// <param name="name">The name value that was supplied to the failed lookup.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31003, Level = LogLevel.Error,
        Message = "Implementation table '{tableName}' (for {typeName}) cannot be resolved by name '{name}' — it has a domain parent join and no Name column; resolve by parent Id (Get(Guid)).")]
    public static partial IGenericMessage ImplementationNotResolvableByName(ILogger logger, string typeName, string tableName, string name);

    /// <summary>
    /// Logs when a record handed to the type-erased Save is not this provider's configuration type.
    /// </summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error, Message = "Cannot save '{actualType}': this provider handles '{expectedType}'")]
    public static partial IGenericMessage UntypedSaveTypeMismatch(ILogger logger, string expectedType, string actualType);

    /// <summary>
    /// Logged when abandoning a half-written configuration record fails. The write's own failure is
    /// what the caller returns, so this is recorded rather than returned -- reporting the cleanup
    /// instead of the cause would hide why the save was abandoned in the first place.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="reason">Why the rollback did not complete.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error,
        Message = "Rolling back a partly written configuration record failed: {reason}")]
    public static partial IGenericMessage WriteScopeRollbackFailed(ILogger logger, string reason);

    /// <summary>
    /// Logged when a registered implementation provider cannot write inside the domain's unit of
    /// work. Reading one does not require it, so the demand is made here rather than at registration.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The record being written.</param>
    /// <param name="providerType">The provider that cannot take part.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91005, Level = LogLevel.Error,
        Message = "Implementation provider '{providerType}' cannot write '{name}' inside the record's unit of work")]
    public static partial IGenericMessage ProviderCannotJoinWrite(ILogger logger, string name, string providerType);


    /// <summary>Reports a missing mapper when detaching cached configuration rows.</summary>
    [MessageLogging(EventId = 61013, Level = LogLevel.Error,
        Message = "Cannot detach cached configuration '{configurationType}': no POCO mapper is registered")]
    public static partial IGenericMessage DetachMapperMissing(ILogger logger, string configurationType);

    /// <summary>Reports a child collection that cannot be composed because its row type has no mapper.</summary>
    [MessageLogging(EventId = 61026, Level = LogLevel.Error,
        Message = "Cannot load the '{childType}' children of '{ownerType}': no POCO mapper is registered for the child type")]
    public static partial IGenericMessage ChildMapperMissing(ILogger logger, string childType, string ownerType);

    /// <summary>
    /// Reports a child query the gateway answered as a success with no result set -- a contract
    /// breach, since an empty collection is an empty list, not an absent one.
    /// </summary>
    [MessageLogging(EventId = 61027, Level = LogLevel.Error,
        Message = "Child query on '{childContainer}' for owner {ownerId} succeeded but returned no result set")]
    public static partial IGenericMessage ChildQueryReturnedNoResult(ILogger logger, string childContainer, string ownerId);

    /// <summary>
    /// Reports a configuration record refused because one of its child collections could not be
    /// loaded. The record fails rather than loading without them: a record missing part of itself
    /// looks complete to every caller, which is how AuthDb once loaded with zero paths.
    /// </summary>
    [MessageLogging(EventId = 61028, Level = LogLevel.Error,
        Message = "Configuration '{configurationType}' refused: its children could not be loaded ({reason})")]
    public static partial IGenericMessage AggregateChildrenUnloaded(ILogger logger, string configurationType, string? reason);

    /// <summary>Identifies the owner and physical join used to load child configuration.</summary>
    // Why Debug: one of these fires per child collection per aggregate load, so a single page of
    // configuration emits dozens. That is mechanism detail for someone tracing a read, not a record
    // of anything the system did on its own behalf, which is what the Information tier is for.
    [MessageLogging(EventId = 11040, Level = LogLevel.Debug,
        Message = "Configuration child read: store='{store}', path='{path}', child='{child}', owner='{owner}', ownerId={ownerId}, foreignKey='{foreignKey}'")]
    public static partial IGenericMessage ChildReadStarting(ILogger logger, string store, string path, string child, string owner, string ownerId, string foreignKey);

    /// <summary>Reports the number of composed child configuration rows.</summary>
    // Why Debug: the counterpart to 11040 and just as frequent. The row count earns its place when
    // someone is diagnosing an aggregate that loaded short, and nowhere else.
    [MessageLogging(EventId = 11041, Level = LogLevel.Debug,
        Message = "Configuration child read completed: child='{child}', ownerId={ownerId}, rows={count}")]
    public static partial IGenericMessage ChildReadCompleted(ILogger logger, string child, string ownerId, int count);

    /// <summary>Reports the join an implementation's rows reach their domain by.</summary>
    [MessageLogging(EventId = 11050, Level = LogLevel.Debug,
        Message = "Implementation '{configType}' in '{container}' reaches domain '{domain}' by column '{foreignKey}' to '{joinColumn}', filtered on '{keyColumn}'")]
    public static partial IGenericMessage DomainJoinResolved(ILogger logger, string configType, string container, string foreignKey, string domain, string joinColumn, string keyColumn);

    /// <summary>Traces an implementation row's children being loaded through its domain.</summary>
    [MessageLogging(EventId = 11051, Level = LogLevel.Trace,
        Message = "Loading the children of implementation '{configType}' in '{container}' through domain '{domain}', domainId={domainId}")]
    public static partial IGenericMessage ImplementationChildrenThroughDomain(ILogger logger, string configType, string container, string domain, Guid domainId);

    /// <summary>Traces a child row being written with the Foreign key values its owner's write returned.</summary>
    [MessageLogging(EventId = 11053, Level = LogLevel.Trace,
        Message = "Writing '{childType}' into '{childContainer}' under '{ownerContainer}' with [{foreignKeyValues}]")]
    public static partial IGenericMessage ChildWritten(ILogger logger, string childType, string childContainer, string ownerContainer, string foreignKeyValues);

    /// <summary>Traces the rows of a container retired under one retired owner row.</summary>
    [MessageLogging(EventId = 11054, Level = LogLevel.Trace,
        Message = "Retired {count} row(s) of '{childContainer}' under '{ownerContainer}' matching [{foreignKeyValues}] (markDeleted={markDeleted})")]
    public static partial IGenericMessage ChildrenRetired(ILogger logger, string childContainer, string ownerContainer, string foreignKeyValues, int count, bool markDeleted);

    /// <summary>Traces the Foreign key a child's values are filled through.</summary>
    [MessageLogging(EventId = 11055, Level = LogLevel.Trace,
        Message = "Foreign key '{keyName}' on '{childContainer}' filled from '{parentContainer}': [{fieldMap}]")]
    public static partial IGenericMessage ForeignKeyFilled(ILogger logger, string keyName, string childContainer, string parentContainer, string fieldMap);

    /// <summary>Traces an implementation row written or retired under its domain row.</summary>
    [MessageLogging(EventId = 11056, Level = LogLevel.Trace,
        Message = "Implementation '{configType}' in '{container}' {operation} under domain '{domainContainer}': {retiredCount} version(s) retired")]
    public static partial IGenericMessage ImplementationUnderDomain(ILogger logger, string configType, string container, string operation, string domainContainer, int retiredCount);

    /// <summary>Traces the domain row a save or delete hangs its implementation from.</summary>
    [MessageLogging(EventId = 11057, Level = LogLevel.Debug,
        Message = "Domain row '{name}' in '{domainContainer}' {state}; key values [{keyValues}]")]
    public static partial IGenericMessage DomainRowResolved(ILogger logger, string name, string domainContainer, string state, string keyValues);

    /// <summary>Logged when a child container declares no Foreign key to the container its owner lives in.</summary>
    [MessageLogging(EventId = 61015, Level = LogLevel.Error,
        Message = "Container '{childContainer}' declares no Foreign key to '{parentContainer}', so its rows cannot be written under it")]
    public static partial IGenericMessage NoForeignKeyToParent(ILogger logger, string childContainer, string parentContainer);

    /// <summary>Logged when a child container declares more than one Foreign key to its owner's container.</summary>
    [MessageLogging(EventId = 61016, Level = LogLevel.Error,
        Message = "Container '{childContainer}' declares {count} Foreign keys to '{parentContainer}'; which one holds the owner is not configured")]
    public static partial IGenericMessage AmbiguousForeignKeyToParent(ILogger logger, string childContainer, string parentContainer, int count);

    /// <summary>Logged when the owner's key values lack the field a child's Foreign key references.</summary>
    [MessageLogging(EventId = 61017, Level = LogLevel.Error,
        Message = "Foreign key '{keyName}' on '{childContainer}' references field '{field}' of '{parentContainer}', which the owner's write did not return")]
    public static partial IGenericMessage ReferencedValueMissing(ILogger logger, string keyName, string childContainer, string parentContainer, string field);

    /// <summary>Logged when a save returns no key values for the row it inserted.</summary>
    [MessageLogging(EventId = 61018, Level = LogLevel.Error,
        Message = "Save into '{container}' returned no key values for the row it wrote, so nothing can be written under it")]
    public static partial IGenericMessage WriteReturnedNoRow(ILogger logger, string container);

    /// <summary>Logged when a child's Foreign key to its owner has more than one field, which the child join cannot express.</summary>
    [MessageLogging(EventId = 61020, Level = LogLevel.Error,
        Message = "Foreign key '{keyName}' on '{childContainer}' to '{parentContainer}' has {fieldCount} fields; the child read joins on one")]
    public static partial IGenericMessage CompositeForeignKeyToParent(ILogger logger, string keyName, string childContainer, string parentContainer, int fieldCount);

    /// <summary>Logged when a name/value child collection reaches the provider without the table its attribute declares.</summary>
    [MessageLogging(EventId = 61021, Level = LogLevel.Error,
        Message = "'{ownerType}.{property}' is a name/value child collection with no [ConfigurationChildTable]; its rows cannot be read or written")]
    public static partial IGenericMessage ChildTableNotDeclared(ILogger logger, string ownerType, string property);

    /// <summary>Traces the domain container an implementation provider was attached to at registration.</summary>
    [MessageLogging(EventId = 11058, Level = LogLevel.Trace,
        Message = "Implementation '{configType}' in '{container}' attached to domain container '{domainContainer}'")]
    public static partial IGenericMessage DomainAttached(ILogger logger, string configType, string container, string domainContainer);

    /// <summary>Logged when an implementation provider is read before any domain provider registered it.</summary>
    [MessageLogging(EventId = 61022, Level = LogLevel.Error,
        Message = "Implementation '{configType}' in '{container}' has no domain container: no domain provider registered it")]
    public static partial IGenericMessage DomainNotAttached(ILogger logger, string configType, string container);

    /// <summary>Logged when a data store declares one container name in more than one of its paths.</summary>
    [MessageLogging(EventId = 61025, Level = LogLevel.Error,
        Message = "Container '{container}' is declared {count} times in data store '{store}'; a container name must be declared once")]
    public static partial IGenericMessage ContainerDeclaredMoreThanOnce(ILogger logger, string container, string store, int count);

    /// <summary>Logged when an implementation provider is asked for by a name nothing registered.</summary>
    [MessageLogging(EventId = 61023, Level = LogLevel.Error,
        Message = "No '{configType}' implementation provider is registered under '{implementation}'")]
    public static partial IGenericMessage ImplementationNotRegistered(ILogger logger, string configType, string implementation);

    /// <summary>Logged when a container a child hangs from declares no Physical key for the child's owner key to reference.</summary>
    [MessageLogging(EventId = 61024, Level = LogLevel.Error,
        Message = "Container '{parent}' declares no Physical key, so no key from '{child}' can hold its rows")]
    public static partial IGenericMessage PhysicalKeyNotDeclared(ILogger logger, string parent, string child);

    /// <summary>Logged when a domain row read back for a save or delete carries no value for its Logical key.</summary>
    [MessageLogging(EventId = 61019, Level = LogLevel.Error,
        Message = "Domain row '{name}' in '{domainContainer}' carries no value for Logical key field '{field}'")]
    public static partial IGenericMessage DomainLogicalValueMissing(ILogger logger, string name, string domainContainer, string field);

    /// <summary>
    /// Logged when an implementation row reaches the child load without the domain Id it was given,
    /// so its children cannot be filtered to it and none are loaded.
    /// </summary>
    [MessageLogging(EventId = 61014, Level = LogLevel.Error,
        Message = "Implementation '{configType}' in '{container}' carries no domain Id, so its children cannot be loaded")]
    public static partial IGenericMessage ImplementationOwnerIdMissing(ILogger logger, string configType, string container);

    /// <summary>
    /// Reports a configuration read refused because a paging or depth argument is out of range. A
    /// page with no rows, a negative offset or a negative depth has no meaning to answer with.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The configuration contract being read.</param>
    /// <param name="name">The argument that is out of range.</param>
    /// <param name="value">The value it was given.</param>
    [MessageLogging(EventId = 20003, Level = LogLevel.Error,
        Message = "{typeName} read refused: '{name}' is out of range ({value})")]
    public static partial IGenericMessage ReadArgumentOutOfRange(ILogger logger, string typeName, string name, int value);

    /// <summary>
    /// Reports a child read refused because the owner type declares no collection of the child type
    /// asked for, so there is no table or key to read it through.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="ownerType">The owner type the children were asked of.</param>
    /// <param name="childType">The child type asked for.</param>
    [MessageLogging(EventId = 61029, Level = LogLevel.Error,
        Message = "'{ownerType}' declares no collection of '{childType}'")]
    public static partial IGenericMessage ChildCollectionNotOnOwner(ILogger logger, string ownerType, string childType);

    /// <summary>
    /// Reports a page of domain rows that came back from the gateway as a success carrying no row set.
    /// An empty page is an empty set; no set at all is not an answer.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The configuration contract being read.</param>
    /// <param name="skip">The page's offset.</param>
    /// <param name="take">The page's size.</param>
    [MessageLogging(EventId = 61030, Level = LogLevel.Error,
        Message = "The {typeName} page at {skip} (size {take}) returned no result")]
    public static partial IGenericMessage DomainPageReturnedNoResult(ILogger logger, string typeName, int skip, int take);

    /// <summary>
    /// Reports a domain record that lists a field its implementation does not carry. A read has
    /// nowhere on the implementation to put the value, and a save has nothing to write it from.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="domainType">The domain record that lists the field.</param>
    /// <param name="implementationType">The implementation that lacks it.</param>
    /// <param name="field">The field.</param>
    [MessageLogging(EventId = 60004, Level = LogLevel.Error,
        Message = "{domainType} lists '{field}', which {implementationType} does not carry")]
    public static partial IGenericMessage ListFieldNotOnImplementation(ILogger logger, string domainType, string implementationType, string field);

    /// <summary>
    /// Reports a registered implementation provider that cannot retire its rows without deleting
    /// them, which a new version of the domain row needs of it.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The member being saved.</param>
    /// <param name="providerType">The provider that cannot supersede.</param>
    [MessageLogging(EventId = 90004, Level = LogLevel.Error,
        Message = "Implementation provider '{providerType}' cannot supersede its rows for '{name}'")]
    public static partial IGenericMessage ProviderCannotSupersede(ILogger logger, string name, string providerType);

    /// <summary>
    /// Reports a count that came back from the gateway as a success carrying no number. A count of
    /// nothing is zero; no count at all is not an answer.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="container">The container that was counted.</param>
    [MessageLogging(EventId = 61030, Level = LogLevel.Error,
        Message = "The count over {container} returned no number")]
    public static partial IGenericMessage CountReturnedNoResult(ILogger logger, string container);

    /// <summary>A page of domain records was read from the domain table alone.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The domain record read.</param>
    /// <param name="skip">The page's offset.</param>
    /// <param name="take">The page's size.</param>
    /// <param name="returned">How many records the page holds.</param>
    /// <param name="total">How many records the query selects in all, or that no total was asked for.</param>
    [MessageLogging(EventId = 11024, Level = LogLevel.Trace,
        Message = "Read {typeName} page at {skip} (size {take}): {returned} row(s), total {total}")]
    public static partial IGenericMessage DomainPageRead(ILogger logger, string typeName, int skip, int take, int returned, string total);
}
