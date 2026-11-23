using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for <c>ConfigurationGatewayProvider</c>.
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class ConfigurationGatewayProviderLog
{
    /// <summary>A gateway was registered for a connection.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection the gateway operates on.</param>
    /// <param name="gatewayType">The gateway's type name.</param>
    [MessageLogging(EventId = 11014, Level = LogLevel.Debug,
        Message = "ConfigurationGatewayProvider: registered '{gatewayType}' for connection '{connectionName}'")]
    public static partial IGenericMessage GatewayRegistered(ILogger logger, string connectionName, string gatewayType);

    /// <summary>A caller asked for a gateway without naming a connection.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 61020, Level = LogLevel.Error,
        Message = "ConfigurationGatewayProvider: no connection name was supplied")]
    public static partial IGenericMessage ConnectionNameMissing(ILogger logger);

    /// <summary>A caller registered a null gateway.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 61021, Level = LogLevel.Error,
        Message = "ConfigurationGatewayProvider: the gateway supplied for registration was null")]
    public static partial IGenericMessage GatewayNull(ILogger logger);

    /// <summary>A gateway was registered without naming the connection it opened.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="gatewayType">The gateway's type name.</param>
    [MessageLogging(EventId = 61023, Level = LogLevel.Error,
        Message = "ConfigurationGatewayProvider: '{gatewayType}' names no connection, so there is nothing to file it under")]
    public static partial IGenericMessage GatewayNamesNoConnection(ILogger logger, string gatewayType);

    /// <summary>A declared connection names no kind, so no factory can be chosen for it.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection as declared.</param>
    [MessageLogging(EventId = 61024, Level = LogLevel.Error,
        Message = "configurationSchema.json declares connection '{connectionName}' with no Implementation, so no connection factory can be chosen for it")]
    public static partial IGenericMessage ConnectionDeclaresNoKind(ILogger logger, string connectionName);

    /// <summary>A caller asked for a connection the schema does not declare.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection asked for.</param>
    [MessageLogging(EventId = 61027, Level = LogLevel.Error,
        Message = "configurationSchema.json declares no connection named '{connectionName}', so no configuration gateway can be built for it")]
    public static partial IGenericMessage ConnectionNotDeclared(ILogger logger, string connectionName);

    /// <summary>A declared connection names a kind no connection option registered.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection as declared.</param>
    /// <param name="implementation">The kind it named.</param>
    [MessageLogging(EventId = 61025, Level = LogLevel.Error,
        Message = "configurationSchema.json declares connection '{connectionName}' as kind '{implementation}', which no connection option registered")]
    public static partial IGenericMessage ConnectionKindNotRegistered(ILogger logger, string connectionName, string implementation);

    /// <summary>The factory a connection kind names is not in the container.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection as declared.</param>
    /// <param name="factoryType">The factory type its kind names.</param>
    [MessageLogging(EventId = 61026, Level = LogLevel.Error,
        Message = "connection '{connectionName}' names factory '{factoryType}', which is not registered — connections must register before configuration gateways")]
    public static partial IGenericMessage ConnectionFactoryUnavailable(ILogger logger, string connectionName, string factoryType);

    /// <summary>The schema declares more than one secret manager, so the gateway cannot choose one.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection being opened.</param>
    /// <param name="declared">The names the schema declares.</param>
    [MessageLogging(EventId = 61028, Level = LogLevel.Error,
        Message = "configurationSchema.json declares {declared} for connection '{connectionName}' to open with, and a gateway cannot choose between them — the secret manager that opens the configuration store is the one the schema declares, so declare exactly one")]
    public static partial IGenericMessage SecretManagerAmbiguous(ILogger logger, string connectionName, string declared);

    /// <summary>A declared secret manager carries no kind.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="secretManagerName">The secret manager as declared.</param>
    [MessageLogging(EventId = 61029, Level = LogLevel.Error,
        Message = "configurationSchema.json declares secret manager '{secretManagerName}' without a Implementation, so no option can be selected to build it")]
    public static partial IGenericMessage SecretManagerDeclaresNoKind(ILogger logger, string secretManagerName);

    /// <summary>A declared secret manager names a kind no secret manager option registered.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="secretManagerName">The secret manager as declared.</param>
    /// <param name="implementation">The kind it named.</param>
    [MessageLogging(EventId = 61030, Level = LogLevel.Error,
        Message = "configurationSchema.json declares secret manager '{secretManagerName}' as kind '{implementation}', which no secret manager option registered")]
    public static partial IGenericMessage SecretManagerKindNotRegistered(ILogger logger, string secretManagerName, string implementation);

    /// <summary>The factory a declared secret manager names is not in the container.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="secretManagerName">The secret manager as declared.</param>
    /// <param name="factoryType">The factory type its kind names.</param>
    [MessageLogging(EventId = 61031, Level = LogLevel.Error,
        Message = "secret manager '{secretManagerName}' names factory '{factoryType}', which is not registered — secret managers must register before configuration gateways")]
    public static partial IGenericMessage SecretManagerFactoryUnavailable(ILogger logger, string secretManagerName, string factoryType);

    /// <summary>A declared secret manager carries no body for its factory to build from.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="secretManagerName">The secret manager as declared.</param>
    [MessageLogging(EventId = 61032, Level = LogLevel.Error,
        Message = "configurationSchema.json declares secret manager '{secretManagerName}' with no Configuration body, and its factory has nothing to build from")]
    public static partial IGenericMessage SecretManagerDeclaresNoBody(ILogger logger, string secretManagerName);

    /// <summary>A declared secret manager is of a kind that cannot be built without the configuration store (FDW-6360).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="secretManagerName">The secret manager as declared.</param>
    /// <param name="implementation">The kind it named.</param>
    [MessageLogging(EventId = 61132, Level = LogLevel.Error,
        Message = "configurationSchema.json declares secret manager '{secretManagerName}' as kind '{implementation}', which cannot be built without the configuration store; declare a kind that is built from its declaration alone, such as EnvironmentVariable")]
    public static partial IGenericMessage SecretManagerNeedsConfigurationStore(ILogger logger, string secretManagerName, string implementation);

    /// <summary>The schema declares two secret managers of one name (FDW-6360).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="secretManagerName">The name declared twice.</param>
    [MessageLogging(EventId = 61133, Level = LogLevel.Error,
        Message = "configurationSchema.json declares secret manager '{secretManagerName}' more than once, so a connection naming it cannot know which is meant")]
    public static partial IGenericMessage SecretManagerDeclaredTwice(ILogger logger, string secretManagerName);

    /// <summary>A connection names a secret manager the schema does not declare (FDW-6360).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection that names it.</param>
    /// <param name="secretManagerName">The manager it names.</param>
    [MessageLogging(EventId = 61134, Level = LogLevel.Error,
        Message = "connection '{connectionName}' names secret manager '{secretManagerName}', which configurationSchema.json does not declare; a connection opened before the configuration store exists can only use a manager the schema declares")]
    public static partial IGenericMessage ConnectionNamesUndeclaredSecretManager(ILogger logger, string connectionName, string secretManagerName);

    /// <summary>The secret a connection's key names could not be read from its declared manager (FDW-6360).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection that needs the secret.</param>
    /// <param name="secretManagerName">The manager it reads from.</param>
    /// <param name="secretKeyName">The key it asks for; never the value.</param>
    /// <param name="reason">The manager's own message, which names the variable or the key and never the value.</param>
    [MessageLogging(EventId = 61135, Level = LogLevel.Error,
        Message = "connection '{connectionName}' needs secret '{secretKeyName}' from secret manager '{secretManagerName}', which could not be read: {reason}")]
    public static partial IGenericMessage ConnectionSecretNotAvailable(ILogger logger, string connectionName, string secretManagerName, string secretKeyName, string reason);

    /// <summary>A secret manager of the schema was asked to be written or changed (FDW-6360).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="operation">What was asked.</param>
    [MessageLogging(EventId = 61136, Level = LogLevel.Error,
        Message = "The secret managers declared in configurationSchema.json are read-only; '{operation}' is refused")]
    public static partial IGenericMessage SchemaSecretManagersAreReadOnly(ILogger logger, string operation);

    /// <summary>A secret manager was asked for by a name the schema does not declare (FDW-6360).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="secretManagerName">The name asked for.</param>
    [MessageLogging(EventId = 61137, Level = LogLevel.Error,
        Message = "secret manager '{secretManagerName}' is not declared in configurationSchema.json")]
    public static partial IGenericMessage SecretManagerNotDeclared(ILogger logger, string secretManagerName);

    /// <summary>A connection that needs a secret names no key to read it by (FDW-6360).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection.</param>
    /// <param name="field">The credential field that names no secret key.</param>
    [MessageLogging(EventId = 61139, Level = LogLevel.Error,
        Message = "connection '{connectionName}' needs a secret for '{field}' but names no secret key to read it by; a login with no password is never built")]
    public static partial IGenericMessage ConnectionNamesNoSecretKey(ILogger logger, string connectionName, string field);

    /// <summary>A connection that needs a secret names no secret manager to read it from (FDW-6360).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection.</param>
    /// <param name="field">The credential field that has a key and no manager.</param>
    [MessageLogging(EventId = 61140, Level = LogLevel.Error,
        Message = "connection '{connectionName}' names a secret key for '{field}' but no secret manager to read it from")]
    public static partial IGenericMessage ConnectionNamesNoSecretManager(ILogger logger, string connectionName, string field);

    /// <summary>The bare, unnamed IConfigurationGateway an endpoint injects directly could not be built.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection <c>ConfigurationConnection</c> named.</param>
    /// <param name="reason">The underlying failure's message.</param>
    [MessageLogging(EventId = 61036, Level = LogLevel.Error,
        Message = "No configuration gateway available for connection '{connectionName}': {reason}")]
    public static partial IGenericMessage BareGatewayUnavailable(ILogger logger, string connectionName, string reason);

    /// <summary>The key chunk size a host set on the configuration gateway collection is below one, so no read could carry a key (FDW-4254).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="keyChunkSize">The value that was set.</param>
    [MessageLogging(EventId = 61039, Level = LogLevel.Error,
        Message = "The configuration gateway key chunk size is {keyChunkSize}; it is the most keys one read carries and must be at least 1")]
    public static partial IGenericMessage KeyChunkSizeInvalid(ILogger logger, int keyChunkSize);

    /// <summary>A data store the host declares in configurationSchema.json names an implementation no DataStoreType carries, so it cannot be built (FDW-6433).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="storeName">The declared store.</param>
    /// <param name="implementation">The implementation it names.</param>
    [MessageLogging(EventId = 61141, Level = LogLevel.Error,
        Message = "data store '{storeName}' declared in configurationSchema.json names implementation '{implementation}', which is not a registered DataStoreType, so the store cannot be built and the host does not start")]
    public static partial IGenericMessage DeclaredStoreImplementationNotRegistered(ILogger logger, string storeName, string implementation);

    /// <summary>A data store the host declares in configurationSchema.json was refused when its builder was configured from the declaration (FDW-6433).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="storeName">The declared store.</param>
    /// <param name="reason">The builder's own refusal, which names the container or the field.</param>
    [MessageLogging(EventId = 61142, Level = LogLevel.Error,
        Message = "data store '{storeName}' declared in configurationSchema.json was refused by its builder, so the host does not start: {reason}")]
    public static partial IGenericMessage DeclaredStoreNotConfigured(ILogger logger, string storeName, string reason);

    /// <summary>A data store the host declares in configurationSchema.json did not build (FDW-6433).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="storeName">The declared store.</param>
    /// <param name="reason">The builder's own refusal, which names the container it stopped on.</param>
    [MessageLogging(EventId = 61143, Level = LogLevel.Error,
        Message = "data store '{storeName}' declared in configurationSchema.json did not build, so the host does not start: {reason}")]
    public static partial IGenericMessage DeclaredStoreNotBuilt(ILogger logger, string storeName, string reason);

    /// <summary>The file configurationSchema.json could not be read (FDW-6433).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="path">The path that was read.</param>
    /// <param name="reason">The reason the read gave.</param>
    [MessageLogging(EventId = 61144, Level = LogLevel.Error,
        Message = "configurationSchema.json at '{path}' could not be read, so the host does not start: {reason}")]
    public static partial IGenericMessage SchemaFileUnreadable(ILogger logger, string path, string reason);

    /// <summary>The file configurationSchema.json does not bind to the schema the host reads (FDW-6433).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="path">The path that was read.</param>
    /// <param name="reason">What the binder said, which names the entry or the value.</param>
    [MessageLogging(EventId = 61145, Level = LogLevel.Error,
        Message = "configurationSchema.json at '{path}' could not be bound, so the host does not start: {reason}")]
    public static partial IGenericMessage SchemaNotBound(ILogger logger, string path, string reason);

    /// <summary>The file configurationSchema.json states no ConfigurationSchema object (FDW-6433).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="path">The path that was read.</param>
    [MessageLogging(EventId = 61146, Level = LogLevel.Error,
        Message = "configurationSchema.json at '{path}' is missing the 'ConfigurationSchema' root object, so the host does not start")]
    public static partial IGenericMessage SchemaRootMissing(ILogger logger, string path);

    /// <summary>The file configurationSchema.json states a key that no class binds, so the binder would drop it without a word (FDW-6584).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="path">The path that was read.</param>
    /// <param name="key">The key as the file spells it.</param>
    /// <param name="place">Where the first one sits, in the file's own words (for example data store 'Platform', path 'tenant', container 'Tenants', field 'Name').</param>
    /// <param name="further">How many more places under the same parent state the same key, as text, or nothing when there are none.</param>
    [MessageLogging(EventId = 61147, Level = LogLevel.Error,
        Message = "configurationSchema.json at '{path}' states the key '{key}', which no class binds, so the host does not start; first at {place}{further}")]
    public static partial IGenericMessage SchemaKeyNotBound(ILogger logger, string path, string key, string place, string further);


}
