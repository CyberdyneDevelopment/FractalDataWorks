using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Logging;

/// <summary>
/// Static logger class for Service operations using MessageLogging infrastructure.
/// </summary>
[MessageLoggingTypeCode("SERVICES")]
public static partial class ServiceLogger
{
    /// <summary>
    /// Logs when configuration cannot be null for a service type.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="serviceTypeName">The name of the service type.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(EventId = 21001, Level = LogLevel.Warning, Message = "Configuration cannot be null for service type '{serviceTypeName}'")]
    public static partial IGenericMessage ConfigurationCannotBeNull(ILogger logger, string serviceTypeName);

    /// <summary>
    /// Logs a generic invalid configuration warning.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="errorMessage">The error message describing the configuration issue.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(EventId = 21002, Level = LogLevel.Warning, Message = "{errorMessage}")]
    public static partial IGenericMessage InvalidConfigurationWarning(ILogger logger, string errorMessage);

    /// <summary>
    /// Logs when validating service configuration.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="serviceTypeName">The name of the service type being validated.</param>
    /// <returns>A generic message containing the debug information.</returns>
    [MessageLogging(EventId = 11017, Level = LogLevel.Debug, Message = "Validating service configuration for '{serviceTypeName}'")]
    public static partial IGenericMessage ValidatingServiceConfiguration(ILogger logger, string serviceTypeName);

    /// <summary>
    /// Logs when a fast generic service is successfully created.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="serviceTypeName">The name of the service type created.</param>
    /// <returns>A generic message containing the information.</returns>
    [MessageLogging(EventId = 11018, Level = LogLevel.Information, Message = "Fast generic service created: '{serviceTypeName}'")]
    public static partial IGenericMessage FastGenericServiceCreated(ILogger logger, string serviceTypeName);

    /// <summary>
    /// Logs when a service has started.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="serviceTypeName">The name of the service type that started.</param>
    /// <returns>A generic message containing the information.</returns>
    [MessageLogging(EventId = 11019, Level = LogLevel.Information, Message = "Service started: '{serviceTypeName}'")]
    public static partial IGenericMessage ServiceStarted(ILogger logger, string serviceTypeName);

    /// <summary>
    /// Logs when fast generic service creation fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="serviceTypeName">The name of the service type that failed to create.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(EventId = 91005, Level = LogLevel.Error, Message = "Fast generic service creation failed for '{serviceTypeName}'")]
    public static partial IGenericMessage FastGenericServiceCreationFailed(ILogger logger, string serviceTypeName);

    /// <summary>
    /// Logs when a service type cast fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sourceType">The source type name.</param>
    /// <param name="targetType">The target type name.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(EventId = 91006, Level = LogLevel.Error, Message = "Service type cast failed: cannot cast '{sourceType}' to '{targetType}'")]
    public static partial IGenericMessage ServiceTypeCastFailed(ILogger logger, string sourceType, string targetType);

    /// <summary>
    /// Logs when factory registration fails due to null type name.
    /// </summary>
    [MessageLogging(EventId = 21003, Level = LogLevel.Error, Message = "Factory registration failed: service type name is null or empty")]
    public static partial IGenericMessage FactoryRegistrationFailedNullTypeName(ILogger logger);

    /// <summary>
    /// Logs when factory registration fails due to null factory.
    /// </summary>
    [MessageLogging(EventId = 21004, Level = LogLevel.Error, Message = "Factory registration failed: factory is null for service type '{serviceType}'")]
    public static partial IGenericMessage FactoryRegistrationFailedNullFactory(ILogger logger, string serviceType);

    /// <summary>
    /// Logs when a factory is registered with the provider.
    /// </summary>
    [MessageLogging(EventId = 11020, Level = LogLevel.Debug, Message = "Factory registered for service type '{serviceType}'")]
    public static partial IGenericMessage ProviderFactoryRegistered(ILogger logger, string serviceType);

    /// <summary>
    /// Logs when service type is required but missing.
    /// </summary>
    [MessageLogging(EventId = 21005, Level = LogLevel.Warning, Message = "Service type is required in configuration")]
    public static partial IGenericMessage ServiceTypeRequired(ILogger logger);

    /// <summary>
    /// Logs when no factory is registered for a service type.
    /// </summary>
    [MessageLogging(EventId = 61003, Level = LogLevel.Warning, Message = "No factory registered for service type '{serviceType}'")]
    public static partial IGenericMessage NoFactoryRegistered(ILogger logger, string serviceType);

    /// <summary>
    /// Logs when configuration is not found.
    /// </summary>
    [MessageLogging(EventId = 30000, Level = LogLevel.Warning, Message = "Configuration not found: '{identifier}'")]
    public static partial IGenericMessage ConfigurationNotFound(ILogger logger, string identifier);

    /// <summary>
    /// Logs when a parent-provider lookup by Id misses; the caller will fall back to a
    /// name-based lookup. Surfaced to FDW013-style failure-path coverage.
    /// </summary>
    [MessageLogging(EventId = 11021, Level = LogLevel.Debug, Message = "Configuration lookup by Id missed for '{identifier}' (domainConfigurationId={domainConfigurationId}); falling back to name lookup")]
    public static partial IGenericMessage ConfigurationLookupByIdMissed(ILogger logger, string identifier, Guid domainConfigurationId);

    /// <summary>
    /// Logs when a service cast fails.
    /// </summary>
    [MessageLogging(EventId = 91007, Level = LogLevel.Warning, Message = "Cast failed: expected '{expectedType}', actual '{actualType}'")]
    public static partial IGenericMessage CastFailed(ILogger logger, string expectedType, string actualType);

    /// <summary>
    /// Logs when configuration provider registration fails due to null type name.
    /// </summary>
    [MessageLogging(EventId = 21006, Level = LogLevel.Error, Message = "Configuration provider registration failed: service type name is null or empty")]
    public static partial IGenericMessage ConfigurationProviderRegistrationFailedNullTypeName(ILogger logger);

    /// <summary>
    /// Logs when configuration provider registration fails due to null provider.
    /// </summary>
    [MessageLogging(EventId = 21007, Level = LogLevel.Error, Message = "Configuration provider registration failed: provider is null for service type '{serviceType}'")]
    public static partial IGenericMessage ConfigurationProviderRegistrationFailedNullProvider(ILogger logger, string serviceType);

    /// <summary>
    /// Logs when a configuration provider is registered with the provider.
    /// </summary>
    [MessageLogging(EventId = 11022, Level = LogLevel.Debug, Message = "Configuration provider registered for service type '{serviceType}'")]
    public static partial IGenericMessage ProviderConfigurationRegistered(ILogger logger, string serviceType);

    /// <summary>
    /// Logs when configuration type is invalid for the provider.
    /// </summary>
    [MessageLogging(EventId = 60003, Level = LogLevel.Warning, Message = "Invalid configuration type: expected '{expectedType}', received '{actualType}'")]
    public static partial IGenericMessage InvalidConfigurationType(ILogger logger, string expectedType, string actualType);

    // ==================== Provider Get Operation Logging (5021-5040) ====================

    /// <summary>
    /// Logs when Get by name starts.
    /// </summary>
    [MessageLogging(EventId = 11023, Level = LogLevel.Debug, Message = "Getting service by name: '{name}'")]
    public static partial IGenericMessage GettingServiceByName(ILogger logger, string name);

    /// <summary>
    /// Logs when Get by ID starts.
    /// </summary>
    [MessageLogging(EventId = 11024, Level = LogLevel.Debug, Message = "Getting service by ID: '{id}'")]
    public static partial IGenericMessage GettingServiceById(ILogger logger, string id);

    /// <summary>
    /// Logs searching a specific service option type for configuration.
    /// </summary>
    [MessageLogging(EventId = 11025, Level = LogLevel.Trace, Message = "  Searching '{serviceOptionType}' configuration provider for '{identifier}'")]
    public static partial IGenericMessage SearchingConfigProvider(ILogger logger, string serviceOptionType, string identifier);

    /// <summary>
    /// Logs when configuration is found in a provider.
    /// </summary>
    [MessageLogging(EventId = 11026, Level = LogLevel.Debug, Message = "  Found configuration in '{serviceOptionType}' provider: '{name}'")]
    public static partial IGenericMessage ConfigurationFoundInProvider(ILogger logger, string serviceOptionType, string name);

    /// <summary>
    /// Logs when configuration is not in a specific provider (Trace - expected during search).
    /// </summary>
    [MessageLogging(EventId = 11027, Level = LogLevel.Trace, Message = "  Not found in '{serviceOptionType}' provider")]
    public static partial IGenericMessage ConfigurationNotInProvider(ILogger logger, string serviceOptionType);

    /// <summary>
    /// Logs when factory lookup succeeds.
    /// </summary>
    [MessageLogging(EventId = 11028, Level = LogLevel.Trace, Message = "  Factory found for '{serviceOptionType}'")]
    public static partial IGenericMessage FactoryLookupSucceeded(ILogger logger, string serviceOptionType);

    /// <summary>
    /// Logs when service is created successfully.
    /// </summary>
    [MessageLogging(EventId = 11029, Level = LogLevel.Debug, Message = "Service created: '{name}' (type: {serviceOptionType})")]
    public static partial IGenericMessage ServiceCreated(ILogger logger, string name, string serviceOptionType);

    /// <summary>
    /// Logs when service creation fails.
    /// </summary>
    [MessageLogging(EventId = 91008, Level = LogLevel.Warning, Message = "Service creation failed for '{name}': {error}")]
    public static partial IGenericMessage ServiceCreationFailed(ILogger logger, string name, string error);

    /// <summary>
    /// Logs the number of registered providers being searched.
    /// </summary>
    [MessageLogging(EventId = 11030, Level = LogLevel.Trace, Message = "Searching {providerCount} configuration provider(s)")]
    public static partial IGenericMessage SearchingProviders(ILogger logger, int providerCount);

    /// <summary>
    /// Logs provider registration summary at startup.
    /// </summary>
    [MessageLogging(EventId = 11031, Level = LogLevel.Debug, Message = "Provider initialized with {factoryCount} factory(ies) and {configProviderCount} configuration provider(s)")]
    public static partial IGenericMessage ProviderInitialized(ILogger logger, int factoryCount, int configProviderCount);

    // ==================== Parent Provider Logging (5031-5035) ====================

    /// <summary>
    /// Logs when a service is resolved via parent config (direct lookup, no provider scan).
    /// </summary>
    [MessageLogging(EventId = 11032, Level = LogLevel.Debug, Message = "Resolved '{name}' via parent config: ServiceOptionType='{serviceOptionType}'")]
    public static partial IGenericMessage ResolvedViaParentConfig(ILogger logger, string name, string serviceOptionType);

    /// <summary>
    /// Logs when no parent provider is registered and a service lookup is attempted.
    /// </summary>
    [MessageLogging(EventId = 61004, Level = LogLevel.Error, Message = "No parent configuration provider registered — cannot resolve '{identifier}'")]
    public static partial IGenericMessage NoDomainConfigurationProviderRegistered(ILogger logger, string identifier);

    /// <summary>
    /// Logs when no configuration provider is registered for a resolved ServiceOptionType.
    /// </summary>
    [MessageLogging(EventId = 61005, Level = LogLevel.Error, Message = "No configuration provider registered for ServiceOptionType '{serviceOptionType}' — cannot resolve '{identifier}'")]
    public static partial IGenericMessage NoConfigurationProviderRegistered(ILogger logger, string identifier, string serviceOptionType);

    /// <summary>
    /// Logs when a parent configuration provider is registered.
    /// </summary>
    [MessageLogging(EventId = 11033, Level = LogLevel.Debug, Message = "Parent configuration provider registered")]
    public static partial IGenericMessage DomainConfigurationProviderRegistered(ILogger logger);

    /// <summary>
    /// Logs when a configuration entry has no ServiceOptionType set.
    /// </summary>
    [MessageLogging(EventId = 60000, Level = LogLevel.Error, Message = "Configuration '{identifier}' has no ServiceOptionType — cannot resolve factory")]
    public static partial IGenericMessage ServiceOptionTypeMissing(ILogger logger, string identifier);

    /// <summary>
    /// Logs the inputs to a typed-configuration lookup before it runs.
    /// </summary>
    [MessageLogging(EventId = 11038, Level = LogLevel.Trace, Message = "Creating '{name}' from typed configuration: ServiceOptionType='{serviceOptionType}', domainConfigurationId={domainConfigurationId}")]
    public static partial IGenericMessage CreatingFromTypedConfiguration(ILogger logger, string name, string serviceOptionType, Guid domainConfigurationId);

    /// <summary>
    /// Logs when the provider was constructed without the container it resolves factories from.
    /// </summary>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error, Message = "Provider '{providerType}' was constructed without a service provider — factories cannot be resolved")]
    public static partial IGenericMessage ContainerNotSupplied(ILogger logger, string providerType);

    /// <summary>
    /// Logs when a configuration's ServiceOptionType matches no registered option in the collection.
    /// </summary>
    [MessageLogging(EventId = 91010, Level = LogLevel.Error, Message = "No registered service type matches ServiceOptionType '{serviceOptionType}' for '{name}'")]
    public static partial IGenericMessage NoServiceTypeForOption(ILogger logger, string serviceOptionType, string name);

    /// <summary>
    /// Logs when an option's factory type is not registered in the container.
    /// </summary>
    [MessageLogging(EventId = 91011, Level = LogLevel.Error, Message = "Factory type '{factoryType}' for '{serviceOptionType}' is not registered in the container")]
    public static partial IGenericMessage FactoryTypeNotResolved(ILogger logger, string factoryType, string serviceOptionType);

    // ── The factory-registry lifecycle ──────────────────────────────────────────────────────────

    /// <summary>
    /// Logs, at the moment an option declares its factory, that the registration is deferred until
    /// the container exists. Trace: one line per option per host — the finest grain there is.
    /// </summary>
    /// <remarks>
    /// <paramref name="declaringType"/> is the type that performed the registration — a BASE class
    /// registering on a derived option's behalf reports itself here, so the base's contribution and
    /// the option's own are distinguishable in the log rather than both reading as "the option".
    /// </remarks>
    [MessageLogging(EventId = 11034, Level = LogLevel.Trace, Message = "Factory registration deferred by {declaringType} for service option '{serviceOptionType}': factory type '{factoryType}' resolves at provider construction")]
    public static partial IGenericMessage FactoryRegistrationDeferred(ILogger logger, string declaringType, string serviceOptionType, string factoryType);

    /// <summary>
    /// Logs each deferred registration as the provider resolves it. Trace: one line per factory per
    /// provider instance, naming what was resolved and into which provider.
    /// </summary>
    [MessageLogging(EventId = 11035, Level = LogLevel.Trace, Message = "{providerType} resolved factory '{factoryType}' for service option '{serviceOptionType}'")]
    public static partial IGenericMessage FactoryResolvedIntoProvider(ILogger logger, string providerType, string factoryType, string serviceOptionType);

    /// <summary>
    /// Logs the outcome of draining the deferred registry into one provider instance. Debug: one
    /// line per provider instance, summarising what Trace listed individually.
    /// </summary>
    [MessageLogging(EventId = 11036, Level = LogLevel.Debug, Message = "{providerType} drained {count} deferred factory registration(s): [{serviceOptionTypes}]")]
    public static partial IGenericMessage ProviderFactoryRegistryDrained(ILogger logger, string providerType, int count, string serviceOptionTypes);

    /// <summary>
    /// Logs that a provider is ready to serve.
    /// </summary>
    [MessageLogging(EventId = 11037, Level = LogLevel.Debug, Message = "{providerType} ready: {count} service option(s) creatable — [{serviceOptionTypes}]")]
    public static partial IGenericMessage ProviderReady(ILogger logger, string providerType, int count, string serviceOptionTypes);

    /// <summary>
    /// Logs that a declared factory type could not be resolved from the container. Warning: the
    /// option is loaded but unusable, which is a real defect for that kind and harmless for the rest.
    /// </summary>
    [MessageLogging(EventId = 61005, Level = LogLevel.Warning, Message = "{providerType}: factory type '{factoryType}' for service option '{serviceOptionType}' did not resolve from the container — '{serviceOptionType}' will not be creatable")]
    public static partial IGenericMessage FactoryTypeUnresolvable(ILogger logger, string providerType, string factoryType, string serviceOptionType);

    /// <summary>
    /// Logs a factory lookup that missed, naming what IS registered. Error: one request failed, and
    /// the registry contents are the single most useful fact for telling "never registered" apart
    /// from "registered under a different discriminator".
    /// </summary>
    [MessageLogging(EventId = 61006, Level = LogLevel.Error, Message = "{providerType} has no factory for service option '{serviceOptionType}' (requested for '{name}'); registered: [{registered}]")]
    public static partial IGenericMessage FactoryLookupMiss(ILogger logger, string providerType, string serviceOptionType, string name, string registered);

    /// <summary>
    /// Logs a provider constructed with an entirely empty factory registry. Critical: not one
    /// service of this domain can ever be created by this instance, so every downstream failure is
    /// a symptom of this line. Fires once per provider instance, never per request.
    /// </summary>
    /// <remarks>
    /// Why Critical rather than Error: an Error says one operation failed; this says the domain is
    /// inoperable for the lifetime of the instance. It is the boot-time condition a host would want
    /// to fail fast on, and the line whose absence made this class of defect invisible.
    /// </remarks>
    [MessageLogging(EventId = 61007, Level = LogLevel.Critical, Message = "{providerType} was constructed with an EMPTY factory registry — no service option of this domain can be created. Every option's registration either never ran or was discarded before the provider was built.")]
    public static partial IGenericMessage ProviderFactoryRegistryEmpty(ILogger logger, string providerType);

}
