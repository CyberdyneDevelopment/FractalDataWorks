using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.DataVault.Logging;

/// <summary>
/// MessageLogging for DataVault operations.
/// EventId range: 4532-4546, plus 4200-4203 (overflow into the DataVault 4200-4249 block;
/// 4547-4549 were vacated to avoid a cross-assembly collision with Credentials core 4547-4549).
/// </summary>
[MessageLoggingTypeCode("DATAVAULT")]
public static partial class DataVaultLog
{
    // ── Vault initialization (connection + pepper resolution, system context) ──

    /// <summary>
    /// Logs that a vault was initialized against the named connection.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="vaultName">The name of the vault that was initialized.</param>
    /// <param name="connectionName">The name of the connection the vault was initialized against.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Debug,
        Message = "Vault '{vaultName}' initialized (connection '{connectionName}')")]
    public static partial IGenericMessage VaultInitialized(ILogger logger, string vaultName, string connectionName);

    /// <summary>
    /// Logs that a vault was used before Initialize() was called in system context.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="vaultName">The name of the vault that has not been initialized.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 41000, Level = LogLevel.Error,
        Message = "Vault '{vaultName}' has not been initialized — call Initialize() in system context before use")]
    public static partial IGenericMessage VaultNotInitialized(ILogger logger, string vaultName);

    /// <summary>
    /// Logs that a vault configuration is missing its ConnectionName.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="vaultName">The name of the vault whose configuration is missing ConnectionName.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61000, Level = LogLevel.Error,
        Message = "Vault '{vaultName}' configuration is missing ConnectionName")]
    public static partial IGenericMessage ConnectionNameMissing(ILogger logger, string vaultName);

    /// <summary>
    /// Logs that a vault configuration is missing its SecretManagerName.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="vaultName">The name of the vault whose configuration is missing SecretManagerName.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61001, Level = LogLevel.Error,
        Message = "Vault '{vaultName}' configuration is missing SecretManagerName")]
    public static partial IGenericMessage SecretManagerNameMissing(ILogger logger, string vaultName);

    /// <summary>
    /// Logs that a vault configuration is missing its PepperSecretName.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="vaultName">The name of the vault whose configuration is missing PepperSecretName.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61002, Level = LogLevel.Error,
        Message = "Vault '{vaultName}' configuration is missing PepperSecretName")]
    public static partial IGenericMessage PepperSecretNameMissing(ILogger logger, string vaultName);

    /// <summary>
    /// Logs that a vault could not resolve its named connection.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="vaultName">The name of the vault that could not resolve its connection.</param>
    /// <param name="connectionName">The name of the connection that could not be resolved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61003, Level = LogLevel.Error,
        Message = "Vault '{vaultName}' could not resolve its connection '{connectionName}'")]
    public static partial IGenericMessage ConnectionResolveFailed(ILogger logger, string vaultName, string connectionName);

    /// <summary>
    /// Logs that a vault could not resolve its named secret manager.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="vaultName">The name of the vault that could not resolve its secret manager.</param>
    /// <param name="secretManagerName">The name of the secret manager that could not be resolved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61004, Level = LogLevel.Error,
        Message = "Vault '{vaultName}' could not resolve secret manager '{secretManagerName}'")]
    public static partial IGenericMessage SecretManagerResolveFailed(ILogger logger, string vaultName, string secretManagerName);

    /// <summary>
    /// Logs that a vault could not read its pepper secret from the named secret manager.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="vaultName">The name of the vault that could not read its pepper.</param>
    /// <param name="secretManagerName">The name of the secret manager the pepper was read from.</param>
    /// <param name="pepperSecretName">The name of the pepper secret that could not be read.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "Vault '{vaultName}' could not read pepper '{pepperSecretName}' from secret manager '{secretManagerName}'")]
    public static partial IGenericMessage PepperReadFailed(ILogger logger, string vaultName, string secretManagerName, string pepperSecretName);

    /// <summary>
    /// Logs that a vault resolved an empty pepper and is refusing to operate.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="vaultName">The name of the vault that resolved an empty pepper.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61005, Level = LogLevel.Error,
        Message = "Vault '{vaultName}' resolved an empty pepper — refusing to operate")]
    public static partial IGenericMessage PepperEmpty(ILogger logger, string vaultName);

    /// <summary>
    /// Logs that a vault rejected a generic command because vaults expose only narrow per-domain verbs.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="vaultName">The name of the vault that rejected the generic command.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 41001, Level = LogLevel.Error,
        Message = "Vault '{vaultName}' rejected a generic command — vaults expose no command surface, only narrow per-domain verbs")]
    public static partial IGenericMessage GenericCommandRejected(ILogger logger, string vaultName);

    // ── Provider / configuration resolution ────────────────────────────────────

    /// <summary>
    /// Logs that a vault request was empty because it supplied neither an Id nor a Name.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error,
        Message = "Empty vault request — request must supply either Id or Name")]
    public static partial IGenericMessage EmptyVaultRequest(ILogger logger);

    /// <summary>
    /// Logs that a typed cache was registered for the given vault ServiceOptionType.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="serviceOptionType">The vault ServiceOptionType the typed cache was registered for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Debug,
        Message = "Registered typed cache for vault ServiceOptionType '{serviceOptionType}'")]
    public static partial IGenericMessage TypedCacheRegistered(ILogger logger, string serviceOptionType);

    /// <summary>
    /// Logs that a vault has no ServiceOptionType, so its typed configuration cannot be resolved.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="vaultName">The name of the vault that has no ServiceOptionType.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61006, Level = LogLevel.Warning,
        Message = "Vault '{vaultName}' has no ServiceOptionType — cannot resolve typed configuration")]
    public static partial IGenericMessage NoServiceOptionType(ILogger logger, string vaultName);

    /// <summary>
    /// Logs that no typed vault provider is registered for the given service option type.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="serviceOptionType">The service option type that has no registered typed provider.</param>
    /// <param name="vaultName">The name of the vault that required the typed provider.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61007, Level = LogLevel.Error,
        Message = "No typed vault provider registered for service option type '{serviceOptionType}' (vault '{vaultName}')")]
    public static partial IGenericMessage NoTypedProviderForServiceOptionType(ILogger logger, string serviceOptionType, string vaultName);

    /// <summary>
    /// Logs that loading the typed vault body failed for the given vault and service option type.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused the typed body load to fail.</param>
    /// <param name="vaultName">The name of the vault whose typed body failed to load.</param>
    /// <param name="serviceOptionType">The service option type of the typed body that failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "Failed to load typed vault body for '{vaultName}' (service option type '{serviceOptionType}')")]
    public static partial IGenericMessage TypedBodyLoadFailed(ILogger logger, System.Exception exception, string vaultName, string serviceOptionType);

    /// <summary>
    /// Logs that the typed vault body was loaded for the given vault and service option type.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="vaultName">The name of the vault whose typed body was loaded.</param>
    /// <param name="serviceOptionType">The service option type of the loaded typed body.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "Typed vault body loaded for '{vaultName}' (service option type '{serviceOptionType}')")]
    public static partial IGenericMessage TypedBodyLoaded(ILogger logger, string vaultName, string serviceOptionType);

    /// <summary>
    /// Logs that a typed configuration lookup has started for the named vault.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="vaultName">The name of the vault whose typed configuration is being looked up.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace,
        Message = "Looking up typed configuration for vault '{vaultName}'")]
    public static partial IGenericMessage TypedLookupStarted(ILogger logger, string vaultName);

    /// <summary>
    /// Logs that the vault factory configuration is invalid because its typed body is missing or of the wrong type.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="vaultName">The name of the vault whose factory configuration is invalid.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61008, Level = LogLevel.Error,
        Message = "Vault factory configuration is invalid for '{vaultName}' — typed body is missing or wrong type")]
    public static partial IGenericMessage FactoryConfigurationInvalid(ILogger logger, string vaultName);

    /// <summary>
    /// Logs that a vault cannot be resolved because the provider's resolution dependencies (connection and secret-manager providers) were not configured via RegisterFactory.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="vaultName">The name of the vault that cannot be resolved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    // Why: the 4532-4549 band is full; 4200 is from the DataVault 4200-4249 allocation block.
    [MessageLogging(EventId = 61009, Level = LogLevel.Error,
        Message = "Vault '{vaultName}' cannot be resolved — the provider's resolution dependencies (connection + secret-manager providers) were not configured via RegisterFactory")]
    public static partial IGenericMessage ResolutionProvidersNotConfigured(ILogger logger, string vaultName);
}
