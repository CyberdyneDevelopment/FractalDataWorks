using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Configuration.Logging;

/// <summary>
/// MessageLogging for ImplementationConfigurationProviderBase operations.
/// EventId range: 9360-9388 (plus 9350)
/// </summary>
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
    /// Logs that a container was not found in a data store for a configuration type and that the lookup falls back to a WHERE Id clause.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="configTypeName">The configuration type name being resolved.</param>
    /// <param name="containerName">The name of the container that was not found.</param>
    /// <param name="dataStoreName">The name of the data store the container was expected in.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning,
        Message = "Container '{containerName}' not found in store '{dataStoreName}' for {configTypeName} — falling back to WHERE [Id]")]
    public static partial IGenericMessage ContainerNotFoundInStore(ILogger logger, string configTypeName, string containerName, string dataStoreName);

    /// <summary>
    /// Logs that no suitable primary or foreign key was found for a container and that the lookup falls back to a WHERE Id clause.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="configTypeName">The configuration type name being resolved.</param>
    /// <param name="containerName">The name of the container that has no suitable key.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31001, Level = LogLevel.Warning,
        Message = "No suitable key (Primary or Foreign) found for container '{containerName}' in {configTypeName} — falling back to WHERE [Id]")]
    public static partial IGenericMessage NoSuitableKeyForContainer(ILogger logger, string configTypeName, string containerName);

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
    /// <param name="serviceOptionType">The discriminator the typed provider was registered for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11007, Level = LogLevel.Trace,
        Message = "Registered typed-body provider for {typeName} discriminator '{serviceOptionType}'")]
    public static partial IGenericMessage TypedProviderRegistered(ILogger logger, string typeName, string serviceOptionType);

    /// <summary>
    /// Logs that the typed body is being loaded for a header using its discriminator.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The header configuration type name whose typed body is being loaded.</param>
    /// <param name="name">The name of the header whose typed body is being loaded.</param>
    /// <param name="serviceOptionType">The discriminator used to load the typed body.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11008, Level = LogLevel.Trace,
        Message = "Loading typed body for {typeName} '{name}' using discriminator '{serviceOptionType}'")]
    public static partial IGenericMessage LoadingTypedBody(ILogger logger, string typeName, string name, string serviceOptionType);

    /// <summary>
    /// Logs that the typed body was successfully loaded and attached to a header.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The header configuration type name whose typed body was loaded.</param>
    /// <param name="name">The name of the header whose typed body was loaded.</param>
    /// <param name="serviceOptionType">The discriminator used to load the typed body.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11009, Level = LogLevel.Trace,
        Message = "Typed body loaded for {typeName} '{name}' (discriminator '{serviceOptionType}')")]
    public static partial IGenericMessage TypedBodyLoaded(ILogger logger, string typeName, string name, string serviceOptionType);

    /// <summary>
    /// Logs that a header carries no discriminator, so its typed body cannot be composed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The header configuration type name missing a discriminator.</param>
    /// <param name="name">The name of the header missing a discriminator.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11010, Level = LogLevel.Debug,
        Message = "Header {typeName} '{name}' has no ServiceOptionType — typed body not composed")]
    public static partial IGenericMessage NoServiceOptionTypeForTypedBody(ILogger logger, string typeName, string name);

    /// <summary>Logs that no implementation configuration provider is registered for a ServiceOptionType.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The configuration's name.</param>
    /// <param name="serviceOptionType">The ServiceOptionType the record names.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61006, Level = LogLevel.Error,
        Message = "No implementation configuration provider registered for ServiceOptionType '{serviceOptionType}' — cannot compose '{name}'")]
    public static partial IGenericMessage NoImplementationProvider(ILogger logger, string name, string serviceOptionType);

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
    /// failed cast at registration time, and the ServiceOptionType may be registered perfectly well.
    /// Sharing one message sent readers to check a registry that was not the problem.
    /// </remarks>
    [MessageLogging(EventId = 61007, Level = LogLevel.Error,
        Message = "Provider offered for '{name}' is not an IServiceConfigurationProvider — it is '{providerType}', so it cannot be registered")]
    public static partial IGenericMessage ProviderNotErasable(ILogger logger, string name, string providerType);

    /// <summary>
    /// Logged when a composed domain record carries no ServiceOptionType to dispatch on.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="identifier">The name or id the record was read for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    /// <remarks>
    /// Also distinct: nothing was looked up here either, because there was nothing to look up with.
    /// The row itself is incomplete, which is a data problem rather than a registration one.
    /// </remarks>
    [MessageLogging(EventId = 61008, Level = LogLevel.Error,
        Message = "Domain record '{identifier}' carries no ServiceOptionType, so no implementation provider can be chosen for it")]
    public static partial IGenericMessage RecordHasNoServiceOptionType(ILogger logger, string identifier);

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
    /// <param name="serviceOptionType">The discriminator for which no typed provider was found.</param>
    /// <param name="name">The name of the header that could not be composed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61002, Level = LogLevel.Error,
        Message = "No typed-body provider registered for {typeName} discriminator '{serviceOptionType}' (header '{name}')")]
    public static partial IGenericMessage NoTypedProviderForServiceOptionType(ILogger logger, string typeName, string serviceOptionType, string name);

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
    /// <param name="serviceOptionType">The discriminator whose typed provider is registered.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 20000, Level = LogLevel.Error,
        Message = "{typeName} '{name}' declares '{serviceOptionType}' but carries no typed body; the aggregate is incomplete and cannot be saved")]
    public static partial IGenericMessage TypedBodyMissingOnSave(ILogger logger, string typeName, string name, string serviceOptionType);

    /// <summary>
    /// Logs that loading the typed body failed for a header.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="exception">The exception that caused the typed body load to fail.</param>
    /// <param name="typeName">The header configuration type name whose typed body failed to load.</param>
    /// <param name="name">The name of the header whose typed body failed to load.</param>
    /// <param name="serviceOptionType">The discriminator used when the load failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71004, Level = LogLevel.Error,
        Message = "Failed to load typed body for {typeName} '{name}' (discriminator '{serviceOptionType}')")]
    public static partial IGenericMessage TypedBodyLoadFailed(ILogger logger, Exception exception, string typeName, string name, string serviceOptionType);

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

    // ── Error (9388) — typed-body name lookup guard ──

    /// <summary>
    /// Logs that a name-based lookup was called on a typed-body table that has a parent FK and
    /// therefore no Name column. The caller must resolve by parent Id (Get(Guid)) instead.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="typeName">The configuration type name that cannot be resolved by name.</param>
    /// <param name="tableName">The table name that has no Name column.</param>
    /// <param name="name">The name value that was supplied to the failed lookup.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31003, Level = LogLevel.Error,
        Message = "Typed-body table '{tableName}' (for {typeName}) cannot be resolved by name '{name}' — it has a parent FK and no Name column; resolve by parent Id (Get(Guid)).")]
    public static partial IGenericMessage TypedBodyNotResolvableByName(ILogger logger, string typeName, string tableName, string name);

    /// <summary>
    /// Logs when a record handed to the type-erased Save is not this provider's configuration type.
    /// </summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error, Message = "Cannot save '{actualType}': this provider handles '{expectedType}'")]
    public static partial IGenericMessage UntypedSaveTypeMismatch(ILogger logger, string expectedType, string actualType);
}
