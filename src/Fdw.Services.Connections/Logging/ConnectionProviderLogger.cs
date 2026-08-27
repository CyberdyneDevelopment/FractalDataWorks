using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Connections.Logging;

/// <summary>
/// Static logger class for connection provider operations.
/// </summary>
[MessageLoggingTypeCode("CONNECTIONS")]
public static partial class ConnectionProviderLogger
{
    /// <summary>
    /// Logs when getting a connection for a specific type.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionType">The type of connection being requested.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "Getting connection for type: {connectionType}")]
    public static partial IGenericMessage GettingConnection(
        ILogger<ConnectionProvider> logger,
        string connectionType);

    /// <summary>
    /// Logs when an unknown connection type is encountered.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionType">The unknown connection type.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 61002,
        Level = LogLevel.Warning,
        Message = "Unknown connection type: {connectionType}")]
    public static partial IGenericMessage UnknownConnectionType(
        ILogger<ConnectionProvider> logger,
        string connectionType);

    /// <summary>
    /// Logs when no factory is registered for a connection type.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionType">The connection type with no factory.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 61003,
        Level = LogLevel.Error,
        Message = "No factory registered for connection type: {connectionType}")]
    public static partial IGenericMessage NoFactoryRegistered(
        ILogger<ConnectionProvider> logger,
        string connectionType);

    /// <summary>
    /// Logs when a connection is successfully created.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionType">The type of connection that was created.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Debug,
        Message = "Successfully created connection for type: {connectionType}")]
    public static partial IGenericMessage ConnectionCreated(
        ILogger<ConnectionProvider> logger,
        string connectionType);

    /// <summary>
    /// Logs when connection creation fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionType">The connection type that failed to create.</param>
    /// <param name="error">The error message.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71001,
        Level = LogLevel.Error,
        Message = "Failed to create connection for type: {connectionType}. Error: {error}")]
    public static partial IGenericMessage ConnectionCreationFailed(
        ILogger<ConnectionProvider> logger,
        string connectionType,
        string error);

    /// <summary>
    /// Logs when connection creation throws an exception.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="connectionType">The connection type that failed to create.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71002,
        Level = LogLevel.Error,
        Message = "Failed to create connection for type {connectionType}")]
    public static partial IGenericMessage ConnectionCreationException(
        ILogger<ConnectionProvider> logger,
        Exception exception,
        string connectionType);

    /// <summary>
    /// Logs when getting a connection by name.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionName">The name of the connection being requested.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Debug,
        Message = "Getting connection by name: {connectionName}")]
    public static partial IGenericMessage GettingConnectionByName(
        ILogger<ConnectionProvider> logger,
        string connectionName);

    /// <summary>
    /// Logs when connection configuration is not found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionName">The name of the connection configuration that was not found.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Error,
        Message = "Connection configuration not found: {connectionName}")]
    public static partial IGenericMessage ConnectionConfigurationNotFound(
        ILogger<ConnectionProvider> logger,
        string connectionName);

    /// <summary>
    /// Logs when GetConnection by ID is not implemented.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="configurationId">The configuration ID that was requested.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Warning,
        Message = "GetConnection by ID is not implemented: {configurationId}")]
    public static partial IGenericMessage GetConnectionByIdNotImplemented(
        ILogger<ConnectionProvider> logger,
        int configurationId);

    /// <summary>
    /// Logs when getting a connection by configuration name.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="configurationName">The name of the configuration being used.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Debug,
        Message = "Getting connection by configuration name: {configurationName}")]
    public static partial IGenericMessage GettingConnectionByConfigurationName(
        ILogger<ConnectionProvider> logger,
        string configurationName);

    /// <summary>
    /// Logs when configuration section is not found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="configurationName">The name of the configuration section that was not found.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 31001,
        Level = LogLevel.Warning,
        Message = "Configuration section not found: Connections:{configurationName}")]
    public static partial IGenericMessage ConfigurationSectionNotFound(
        ILogger<ConnectionProvider> logger,
        string configurationName);

    /// <summary>
    /// Logs when ConnectionType is not specified in configuration.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="configurationName">The name of the configuration with missing ConnectionType.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 61004,
        Level = LogLevel.Warning,
        Message = "ConnectionType not specified in configuration section: {configurationName}")]
    public static partial IGenericMessage ConnectionTypeNotSpecified(
        ILogger<ConnectionProvider> logger,
        string configurationName);

    /// <summary>
    /// Logs when unknown connection type is found in configuration.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionType">The unknown connection type from configuration.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 61005,
        Level = LogLevel.Warning,
        Message = "Unknown connection type in configuration: {connectionType}")]
    public static partial IGenericMessage UnknownConnectionTypeInConfiguration(
        ILogger<ConnectionProvider> logger,
        string connectionType);

    /// <summary>
    /// Logs when configuration binding fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="configurationType">The type that configuration failed to bind to.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Failed to bind configuration section to type: {configurationType}")]
    public static partial IGenericMessage ConfigurationBindingFailed(
        ILogger<ConnectionProvider> logger,
        string? configurationType);

    /// <summary>
    /// Logs when getting connection by configuration name fails with exception.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="configurationName">The configuration name that failed.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71003,
        Level = LogLevel.Error,
        Message = "Failed to get connection by configuration name: {configurationName}")]
    public static partial IGenericMessage GetConnectionByNameException(
        ILogger<ConnectionProvider> logger,
        Exception exception,
        string configurationName);

    /// <summary>
    /// Logs when attempting to get a typed connection.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="targetType">The target type being requested.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Debug,
        Message = "Attempting to get connection as type: {targetType}")]
    public static partial IGenericMessage AttemptingTypedConnection(
        ILogger<ConnectionProvider> logger,
        string targetType);

    /// <summary>
    /// Logs when connection cast to specific type succeeds.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="targetType">The target type that was successfully cast to.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Debug,
        Message = "Successfully cast connection to type: {targetType}")]
    public static partial IGenericMessage ConnectionCastSucceeded(
        ILogger<ConnectionProvider> logger,
        string targetType);

    /// <summary>
    /// Logs when connection cast to specific type fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="targetType">The target type that failed to cast to.</param>
    /// <param name="actualType">The actual type of the connection.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 91002,
        Level = LogLevel.Warning,
        Message = "Failed to cast connection to type: {targetType}. Actual type: {actualType}")]
    public static partial IGenericMessage ConnectionCastFailed(
        ILogger<ConnectionProvider> logger,
        string targetType,
        string actualType);

    /// <summary>
    /// Logs when getting a data connection by name.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionName">The name of the data connection being requested.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Debug,
        Message = "Getting data connection by name: {connectionName}")]
    public static partial IGenericMessage GettingDataConnection(
        ILogger<ConnectionProvider> logger,
        string connectionName);

    /// <summary>
    /// Logs when a connection does not support IDataConnection.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionName">The name of the connection.</param>
    /// <param name="actualType">The actual type of the connection.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 41000,
        Level = LogLevel.Error,
        Message = "Connection '{connectionName}' does not implement IDataConnection. Actual type: {actualType}")]
    public static partial IGenericMessage ConnectionNotDataConnection(
        ILogger<ConnectionProvider> logger,
        string connectionName,
        string actualType);

    /// <summary>
    /// Logs when starting to load connection configurations from appsettings.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Debug,
        Message = "Loading connection configurations from Connections section")]
    public static partial IGenericMessage LoadingConnectionConfigurations(ILogger<ConnectionProvider> logger);

    /// <summary>
    /// Logs when a connection configuration is successfully loaded.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionName">The name of the connection.</param>
    /// <param name="connectionType">The type of connection.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Information,
        Message = "Loaded connection configuration '{connectionName}' (type: {connectionType})")]
    public static partial IGenericMessage ConnectionConfigurationLoaded(
        ILogger<ConnectionProvider> logger,
        string connectionName,
        string connectionType);

    /// <summary>
    /// Logs when all connection configurations have been loaded.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of configurations loaded.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Information,
        Message = "Loaded {count} connection configuration(s)")]
    public static partial IGenericMessage ConnectionConfigurationsLoaded(
        ILogger<ConnectionProvider> logger,
        int count);

    /// <summary>
    /// Logs when connection is successfully retrieved from cache.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionName">The name of the connection.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Debug,
        Message = "Retrieved connection configuration '{connectionName}' from cache")]
    public static partial IGenericMessage ConnectionConfigurationRetrievedFromCache(
        ILogger<ConnectionProvider> logger,
        string connectionName);

    /// <summary>
    /// Logs when creating a connection using factory.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionName">The name of the connection.</param>
    /// <param name="factoryType">The factory type being used.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Debug,
        Message = "Creating connection '{connectionName}' using factory {factoryType}")]
    public static partial IGenericMessage CreatingConnectionWithFactory(
        ILogger<ConnectionProvider> logger,
        string connectionName,
        string factoryType);

    /// <summary>
    /// Logs when the Connections section is not found in configuration.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 31002,
        Level = LogLevel.Warning,
        Message = "Connections section not found in configuration")]
    public static partial IGenericMessage ConnectionsSectionNotFound(ILogger<ConnectionProvider> logger);

    /// <summary>
    /// Logs when a connection factory is not registered in DI.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionName">The name of the connection.</param>
    /// <param name="factoryType">The factory type that was not found.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 61006,
        Level = LogLevel.Error,
        Message = "Factory for connection '{connectionName}' not registered in DI. Expected type: {factoryType}")]
    public static partial IGenericMessage FactoryNotRegisteredInDi(
        ILogger<ConnectionProvider> logger,
        string connectionName,
        string factoryType);

    /// <summary>
    /// Logs when the provider subscribes to configuration change notifications.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 7128,
        Level = LogLevel.Debug,
        Message = "Subscribed to connection configuration change notifications")]
    public static partial void SubscribedToConfigurationChanges(ILogger<ConnectionProvider> logger);

    /// <summary>
    /// Logs when configuration changes are detected.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 7129,
        Level = LogLevel.Information,
        Message = "Connection configuration changed, clearing cache")]
    public static partial void ConfigurationChanged(ILogger<ConnectionProvider> logger);

    /// <summary>
    /// Logs when the connection cache is cleared.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of cache entries cleared.</param>
    [LoggerMessage(
        EventId = 7130,
        Level = LogLevel.Debug,
        Message = "Cleared {count} cached connection configuration(s)")]
    public static partial void CacheCleared(ILogger<ConnectionProvider> logger, int count);

    /// <summary>
    /// Logs when configuration loading fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionName">The connection name that failed to load.</param>
    /// <param name="typeName">The type name of the configuration.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71004,
        Level = LogLevel.Error,
        Message = "Failed to load configuration '{connectionName}' for type '{typeName}'")]
    public static partial IGenericMessage ConfigurationLoadFailed(
        ILogger<ConnectionProvider> logger,
        string connectionName,
        string typeName);

    /// <summary>
    /// Logs when factory creation fails with the underlying reason.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="factoryType">The factory type that was used.</param>
    /// <param name="reason">The underlying reason for failure.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71005,
        Level = LogLevel.Error,
        Message = "Factory '{factoryType}' failed to create connection '{connectionName}': {reason}")]
    public static partial IGenericMessage FactoryCreationFailed(
        ILogger<ConnectionProvider> logger,
        string connectionName,
        string factoryType,
        string reason);

    /// <summary>
    /// Logs when a connection is successfully created.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="connectionType">The type of connection created.</param>
    [LoggerMessage(
        EventId = 7134,
        Level = LogLevel.Information,
        Message = "Connection '{ConnectionName}' created successfully (type: {ConnectionType})")]
    public static partial void ConnectionCreatedSuccessfully(
        ILogger<ConnectionProvider> logger,
        string connectionName,
        string connectionType);

    // ═══════════════════════════════════════════════════════════════════════════
    // Trace-Level Diagnostic Events (7140-7160)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Traces when starting to resolve a connection factory.
    /// </summary>
    [LoggerMessage(
        EventId = 7140,
        Level = LogLevel.Trace,
        Message = "Resolving factory for connection type '{ConnectionType}'")]
    public static partial void ResolvingFactory(
        ILogger<ConnectionProvider> logger,
        string connectionType);

    /// <summary>
    /// Traces when factory resolution succeeds.
    /// </summary>
    [LoggerMessage(
        EventId = 7141,
        Level = LogLevel.Trace,
        Message = "Factory resolved for connection type '{ConnectionType}': {FactoryType}")]
    public static partial void FactoryResolved(
        ILogger<ConnectionProvider> logger,
        string connectionType,
        string factoryType);

    /// <summary>
    /// Traces when looking up configuration by name.
    /// </summary>
    [LoggerMessage(
        EventId = 7142,
        Level = LogLevel.Trace,
        Message = "Looking up configuration for connection '{ConnectionName}'")]
    public static partial void LookingUpConfiguration(
        ILogger<ConnectionProvider> logger,
        string connectionName);

    /// <summary>
    /// Traces when configuration is found.
    /// </summary>
    [LoggerMessage(
        EventId = 7143,
        Level = LogLevel.Trace,
        Message = "Configuration found for connection '{ConnectionName}': Type='{ConnectionType}'")]
    public static partial void ConfigurationFound(
        ILogger<ConnectionProvider> logger,
        string connectionName,
        string connectionType);

    /// <summary>
    /// Traces available connections in provider.
    /// </summary>
    [LoggerMessage(
        EventId = 7144,
        Level = LogLevel.Trace,
        Message = "Available connections: [{ConnectionNames}]")]
    public static partial void AvailableConnections(
        ILogger<ConnectionProvider> logger,
        string connectionNames);

    /// <summary>
    /// Traces when starting factory registration with provider.
    /// </summary>
    [LoggerMessage(
        EventId = 7145,
        Level = LogLevel.Trace,
        Message = "Registering factory '{FactoryType}' for connection type '{ConnectionType}'")]
    public static partial void RegisteringFactory(
        ILogger<ConnectionProvider> logger,
        string factoryType,
        string connectionType);

    /// <summary>
    /// Traces when configuration provider is registered.
    /// </summary>
    [LoggerMessage(
        EventId = 7146,
        Level = LogLevel.Trace,
        Message = "Registering configuration provider for connection type '{ConnectionType}'")]
    public static partial void RegisteringConfigurationProvider(
        ILogger<ConnectionProvider> logger,
        string connectionType);

    /// <summary>
    /// Traces when connection factory Create method is called.
    /// </summary>
    [LoggerMessage(
        EventId = 7147,
        Level = LogLevel.Trace,
        Message = "Factory.Create called for connection '{ConnectionName}' (type: {ConnectionType})")]
    public static partial void FactoryCreateCalled(
        ILogger<ConnectionProvider> logger,
        string connectionName,
        string connectionType);

    /// <summary>
    /// Traces when connection pool is accessed.
    /// </summary>
    [LoggerMessage(
        EventId = 7148,
        Level = LogLevel.Trace,
        Message = "Accessing connection pool for '{ConnectionName}'")]
    public static partial void AccessingConnectionPool(
        ILogger<ConnectionProvider> logger,
        string connectionName);

    /// <summary>
    /// Traces IOptionsMonitor configuration count.
    /// </summary>
    [LoggerMessage(
        EventId = 7149,
        Level = LogLevel.Trace,
        Message = "IOptionsMonitor contains {Count} connection configuration(s)")]
    public static partial void OptionsMonitorCount(
        ILogger<ConnectionProvider> logger,
        int count);

    /// <summary>
    /// Traces when iterating over configuration providers.
    /// </summary>
    [LoggerMessage(
        EventId = 7150,
        Level = LogLevel.Trace,
        Message = "Searching {Count} configuration provider(s) for connection '{ConnectionName}'")]
    public static partial void SearchingConfigurationProviders(
        ILogger<ConnectionProvider> logger,
        int count,
        string connectionName);

    /// <summary>
    /// Traces when connection configuration binding starts.
    /// </summary>
    [LoggerMessage(
        EventId = 7151,
        Level = LogLevel.Trace,
        Message = "Binding configuration section '{SectionName}' for connection type '{ConnectionType}'")]
    public static partial void BindingConfigurationSection(
        ILogger<ConnectionProvider> logger,
        string sectionName,
        string connectionType);

    // ═══════════════════════════════════════════════════════════════════════════
    // Connection Instance Cache Events (7155-7160)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Traces when a connection is retrieved from the instance cache.
    /// </summary>

    /// <summary>
    /// Logs when a connection is not found in cache and will be created.
    /// </summary>

    /// <summary>
    /// Logs when the connection instance cache is invalidated due to configuration change.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entriesCleared">The number of cache entries that were cleared.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Information,
        Message = "Connection instance cache invalidated, cleared {entriesCleared} cached connection(s)")]
    public static partial IGenericMessage ConnectionCacheInvalidated(
        ILogger<ConnectionProvider> logger,
        int entriesCleared);

    /// <summary>
    /// Logs when a single cached connection entry is evicted and disposed.
    /// </summary>

    /// <summary>
    /// Logs when disposing a cached connection fails.
    /// </summary>

    /// <summary>
    /// Logs when a stale connection is evicted from the cache.
    /// </summary>

    /// <summary>
    /// Logs when parent provider registration fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="error">The error message from the failed result.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 61007,
        Level = LogLevel.Error,
        Message = "Failed to register parent configuration provider: {error}")]
    public static partial IGenericMessage DomainConfigurationProviderRegistrationFailed(
        ILogger<ConnectionProvider> logger,
        string error);

    // ═══════════════════════════════════════════════════════════════════════════
    // Composed-Header Factory Path Events (7165-7169)
    // Why: After config-split, typed bodies are attached to the composed header via
    // ConnectionConfigurationProvider.PopulateTypedBody. ConnectionProvider
    // extracts header.Configuration and passes it directly to the factory — bypassing
    // DefaultServiceProvider.CreateFromType which would look up _configurationProviders
    // (now empty). These events trace that new path.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when a connection header has no Configuration after PopulateTypedBody.</summary>
    [LoggerMessage(
        EventId = 7165,
        Level = LogLevel.Warning,
        Message = "Connection header '{ConnectionName}' (type '{ServiceOptionType}') has no typed body configuration — cannot create connection")]
    public static partial void ComposedHeaderNoConfiguration(
        ILogger<ConnectionProvider> logger,
        string connectionName,
        string serviceOptionType);

    /// <summary>Logs when no factory is registered for the service option type on the composed-header path.</summary>
    [LoggerMessage(
        EventId = 7166,
        Level = LogLevel.Error,
        Message = "No factory registered for service option type '{ServiceOptionType}' (connection '{ConnectionName}') on composed-header path")]
    public static partial void ComposedHeaderNoFactory(
        ILogger<ConnectionProvider> logger,
        string serviceOptionType,
        string connectionName);

    /// <summary>Traces the composed-header factory.Create call.</summary>
    [LoggerMessage(
        EventId = 7167,
        Level = LogLevel.Trace,
        Message = "Creating connection '{ConnectionName}' via composed-header factory (type '{ServiceOptionType}', body '{BodyType}')")]
    public static partial void ComposedHeaderCreating(
        ILogger<ConnectionProvider> logger,
        string connectionName,
        string serviceOptionType,
        string bodyType);

    /// <summary>Traces successful connection creation on the composed-header path.</summary>
    [LoggerMessage(
        EventId = 7168,
        Level = LogLevel.Trace,
        Message = "Connection '{ConnectionName}' created from composed header (type '{ServiceOptionType}')")]
    public static partial void ComposedHeaderCreated(
        ILogger<ConnectionProvider> logger,
        string connectionName,
        string serviceOptionType);

    /// <summary>
    /// Logs when the provider subscribes to system (ctrl) configuration change notifications.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 7163,
        Level = LogLevel.Debug,
        Message = "Subscribed to system connection configuration change notifications")]
    public static partial void SubscribedToSystemConfigurationChanges(ILogger<ConnectionProvider> logger);

    /// <summary>
    /// Logs when a connection header carries no ServiceOptionType, so there is nothing to dispatch on.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionName">The connection being resolved.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71006,
        Level = LogLevel.Error,
        Message = "Cannot resolve connection '{connectionName}' — its configuration header declares no ServiceOptionType, so no connection kind can be selected")]
    public static partial IGenericMessage ServiceOptionTypeMissing(
        ILogger<ConnectionProvider> logger,
        string connectionName);

    /// <summary>
    /// Logs when a connection header has a ServiceOptionType but no typed body was composed onto it.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionName">The connection being resolved.</param>
    /// <param name="serviceOptionType">The connection's ServiceOptionType.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71011,
        Level = LogLevel.Error,
        Message = "Cannot resolve connection '{connectionName}' — its '{serviceOptionType}' typed body is missing from the composed header")]
    public static partial IGenericMessage TypedBodyMissing(
        ILogger<ConnectionProvider> logger,
        string connectionName,
        string serviceOptionType);

    /// <summary>
    /// Logs when the factory registered for a connection's ServiceOptionType does not implement
    /// <see cref="Fdw.Services.Connections.Abstractions.IConnectionFactory"/>, so it cannot receive
    /// the FDW secret-manager provider.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionName">The connection being resolved.</param>
    /// <param name="serviceOptionType">The connection's ServiceOptionType.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71007,
        Level = LogLevel.Error,
        Message = "Cannot resolve connection '{connectionName}' — factory for '{serviceOptionType}' does not implement IConnectionFactory")]
    public static partial IGenericMessage FactoryNotConnectionFactory(
        ILogger<ConnectionProvider> logger,
        string connectionName,
        string serviceOptionType);

    /// <summary>
    /// Logs when a connection is requested before the header configuration provider has been wired
    /// (phase 3 of the domain's three-phase registration has not run yet).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="identifier">The name or id being resolved.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71008,
        Level = LogLevel.Error,
        Message = "Cannot resolve connection '{identifier}' — no header configuration provider registered (RegisterDomainConfigurationProvider has not run)")]
    public static partial IGenericMessage DomainConfigurationProviderNotRegistered(
        ILogger<ConnectionProvider> logger,
        string identifier);

    /// <summary>
    /// Logs when a caller supplies a connection configuration with no Name. The connection cache is
    /// name-keyed, so a nameless configuration cannot be resolved or shared.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionId">The configuration's Id.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71009,
        Level = LogLevel.Error,
        Message = "Connection configuration (Id: {connectionId}) has no Name — cannot resolve a connection from it")]
    public static partial IGenericMessage ConnectionConfigurationNameMissing(
        ILogger<ConnectionProvider> logger,
        string connectionId);

    /// <summary>
    /// Logs when a freshly created connection reports itself stale immediately after the stale cache
    /// entry was evicted and rebuilt — a factory or configuration defect, not a cache problem.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionName">The connection being resolved.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71010,
        Level = LogLevel.Error,
        Message = "Connection '{connectionName}' is stale immediately after creation — the factory or its configuration is producing unusable connections")]
    public static partial IGenericMessage ConnectionStaleOnCreation(
        ILogger<ConnectionProvider> logger,
        string connectionName);

    /// <summary>
    /// Logs the start of the domain configuration cascade. Trace: the finest grain — it fires once
    /// per option that calls the cascade, and the cascade is idempotent, so repeats are expected
    /// and are themselves the evidence that every option is calling it.
    /// </summary>
    [MessageLogging(
        EventId = 11016,
        Level = LogLevel.Trace,
        Message = "{providerType}: registering domain configuration (header provider, typed-config abstractions, health checkable, health service)")]
    public static partial IGenericMessage DomainConfigurationRegistering(
        ILogger<ConnectionConfigurationProvider> logger,
        string providerType);

    /// <summary>
    /// Logs completion of the domain configuration cascade. Debug: one line summarising what the
    /// Trace above announced, so a reader at Debug sees the step happened without the per-call noise.
    /// </summary>
    [MessageLogging(
        EventId = 11017,
        Level = LogLevel.Debug,
        Message = "{providerType}: domain configuration registered (idempotent — first caller wins)")]
    public static partial IGenericMessage DomainConfigurationRegistered(
        ILogger<ConnectionConfigurationProvider> logger,
        string providerType);
}
