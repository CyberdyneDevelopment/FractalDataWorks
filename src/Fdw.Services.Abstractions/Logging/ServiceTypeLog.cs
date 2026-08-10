using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.ServiceTypes.Logging;

/// <summary>
/// Static logger class for ServiceType registration operations.
/// Uses MessageLogging source generator for high-performance structured logging.
/// </summary>
[MessageLoggingTypeCode("ABSTRACTIONS3")]
public static partial class ServiceTypeLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Collection-Level Events (8001-8020)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when Register starts for a collection.
    /// </summary>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Information,
        Message = "[{collectionName}] Starting Register with {count} service type(s)")]
    public static partial IGenericMessage RegisterStarting(
        ILogger logger,
        string collectionName,
        int count);

    /// <summary>
    /// Logs when Register completes for a collection.
    /// </summary>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Information,
        Message = "[{collectionName}] Completed Register - {count} service type(s) registered")]
    public static partial IGenericMessage RegisterCompleted(
        ILogger logger,
        string collectionName,
        int count);

    /// <summary>
    /// Logs when Configure starts for a collection.
    /// </summary>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Information,
        Message = "[{collectionName}] Starting Configure with {count} service type(s)")]
    public static partial IGenericMessage ConfigureStarting(
        ILogger logger,
        string collectionName,
        int count);

    /// <summary>
    /// Logs when Configure completes for a collection.
    /// </summary>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Information,
        Message = "[{collectionName}] Completed Configure - {count} service type(s) configured")]
    public static partial IGenericMessage ConfigureCompleted(
        ILogger logger,
        string collectionName,
        int count);

    /// <summary>
    /// Logs when Initialize starts for a collection.
    /// </summary>
    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Information,
        Message = "[{collectionName}] Starting Initialize with {count} service type(s)")]
    public static partial IGenericMessage InitializeStarting(
        ILogger logger,
        string collectionName,
        int count);

    /// <summary>
    /// Logs when Initialize completes for a collection.
    /// </summary>
    [MessageLogging(
        EventId = 11016,
        Level = LogLevel.Information,
        Message = "[{collectionName}] Completed Initialize - {count} factory(s) initialized")]
    public static partial IGenericMessage InitializeCompleted(
        ILogger logger,
        string collectionName,
        int count);

    /// <summary>
    /// Logs when a ServiceType is registered via RegisterMember.
    /// </summary>
    [MessageLogging(
        EventId = 11017,
        Level = LogLevel.Debug,
        Message = "[{collectionName}] RegisterMember called for '{serviceTypeName}' (pending count: {pendingCount})")]
    public static partial IGenericMessage RegisterMemberCalled(
        ILogger logger,
        string collectionName,
        string serviceTypeName,
        int pendingCount);

    /// <summary>
    /// Logs when the collection freezes.
    /// </summary>
    [MessageLogging(
        EventId = 11018,
        Level = LogLevel.Debug,
        Message = "[{collectionName}] Collection freezing with {count} service type(s)")]
    public static partial IGenericMessage CollectionFreezing(
        ILogger logger,
        string collectionName,
        int count);

    /// <summary>
    /// Logs when the collection has been frozen.
    /// </summary>
    [MessageLogging(
        EventId = 11019,
        Level = LogLevel.Information,
        Message = "[{collectionName}] Collection frozen with {count} service type(s): {typeNames}")]
    public static partial IGenericMessage CollectionFrozen(
        ILogger logger,
        string collectionName,
        int count,
        string typeNames);

    /// <summary>
    /// Logs when RegisterMember is rejected because collection is frozen.
    /// </summary>
    [MessageLogging(
        EventId = 41000,
        Level = LogLevel.Warning,
        Message = "[{collectionName}] RegisterMember rejected for '{serviceTypeName}' - collection is frozen")]
    public static partial IGenericMessage RegisterMemberRejected(
        ILogger logger,
        string collectionName,
        string serviceTypeName);

    /// <summary>
    /// Logs when a duplicate ServiceType is skipped during registration.
    /// </summary>
    [MessageLogging(
        EventId = 11020,
        Level = LogLevel.Debug,
        Message = "[{collectionName}] Skipping duplicate registration for '{serviceTypeName}'")]
    public static partial IGenericMessage DuplicateRegistrationSkipped(
        ILogger logger,
        string collectionName,
        string serviceTypeName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Type-Level Events (8021-8040)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when registering required services for a ServiceType.
    /// </summary>
    [MessageLogging(
        EventId = 11021,
        Level = LogLevel.Debug,
        Message = "[{serviceTypeName}] Registering required services")]
    public static partial IGenericMessage RegisteringRequiredServices(
        ILogger logger,
        string serviceTypeName);

    /// <summary>
    /// Logs when required services registration completes.
    /// </summary>
    [MessageLogging(
        EventId = 11022,
        Level = LogLevel.Information,
        Message = "[{serviceTypeName}] Registered required services (factory: {factoryTypeName})")]
    public static partial IGenericMessage RequiredServicesRegistered(
        ILogger logger,
        string serviceTypeName,
        string factoryTypeName);

    /// <summary>
    /// Logs when configuring a ServiceType.
    /// </summary>
    [MessageLogging(
        EventId = 11023,
        Level = LogLevel.Debug,
        Message = "[{serviceTypeName}] Configuring from section '{sectionName}'")]
    public static partial IGenericMessage ConfiguringServiceType(
        ILogger logger,
        string serviceTypeName,
        string sectionName);

    /// <summary>
    /// Logs when ServiceType configuration completes.
    /// </summary>
    [MessageLogging(
        EventId = 11024,
        Level = LogLevel.Information,
        Message = "[{serviceTypeName}] Configuration bound from section '{sectionName}'")]
    public static partial IGenericMessage ServiceTypeConfigured(
        ILogger logger,
        string serviceTypeName,
        string sectionName);

    /// <summary>
    /// Logs when registering a factory with the provider.
    /// </summary>
    [MessageLogging(
        EventId = 11025,
        Level = LogLevel.Information,
        Message = "[{collectionName}] Registering '{serviceTypeName}' factory with {providerTypeName}")]
    public static partial IGenericMessage RegisteringFactory(
        ILogger logger,
        string collectionName,
        string serviceTypeName,
        string providerTypeName);

    /// <summary>
    /// Logs when factory registration completes.
    /// </summary>
    [MessageLogging(
        EventId = 11026,
        Level = LogLevel.Information,
        Message = "[{collectionName}] Registered '{serviceTypeName}' - factory: {factoryTypeName}")]
    public static partial IGenericMessage FactoryRegistered(
        ILogger logger,
        string collectionName,
        string serviceTypeName,
        string factoryTypeName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error Events (8041-8060)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when service registration fails.
    /// </summary>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Error,
        Message = "[{serviceTypeName}] Failed to register required services: {error}")]
    public static partial IGenericMessage RegistrationFailed(
        ILogger logger,
        string serviceTypeName,
        string error);

    /// <summary>
    /// Logs when service registration throws an exception.
    /// </summary>
    [MessageLogging(
        EventId = 61001,
        Level = LogLevel.Error,
        Message = "[{serviceTypeName}] Exception during service registration")]
    public static partial IGenericMessage RegistrationException(
        ILogger logger,
        Exception exception,
        string serviceTypeName);

    /// <summary>
    /// Logs when configuration binding fails.
    /// </summary>
    [MessageLogging(
        EventId = 61002,
        Level = LogLevel.Error,
        Message = "[{serviceTypeName}] Failed to bind configuration from section '{sectionName}': {error}")]
    public static partial IGenericMessage ConfigurationFailed(
        ILogger logger,
        string serviceTypeName,
        string sectionName,
        string error);

    /// <summary>
    /// Logs when configuration binding throws an exception.
    /// </summary>
    [MessageLogging(
        EventId = 61003,
        Level = LogLevel.Error,
        Message = "[{serviceTypeName}] Exception during configuration binding from section '{sectionName}'")]
    public static partial IGenericMessage ConfigurationException(
        ILogger logger,
        Exception exception,
        string serviceTypeName,
        string sectionName);

    /// <summary>
    /// Logs when factory registration fails.
    /// </summary>
    [MessageLogging(
        EventId = 61004,
        Level = LogLevel.Error,
        Message = "[{serviceTypeName}] Failed to register factory: {error}")]
    public static partial IGenericMessage FactoryRegistrationFailed(
        ILogger logger,
        string serviceTypeName,
        string error);

    /// <summary>
    /// Logs when factory registration throws an exception.
    /// </summary>
    [MessageLogging(
        EventId = 61005,
        Level = LogLevel.Error,
        Message = "[{serviceTypeName}] Exception during factory registration")]
    public static partial IGenericMessage FactoryRegistrationException(
        ILogger logger,
        Exception exception,
        string serviceTypeName);

    /// <summary>
    /// Logs when factory resolution from DI fails.
    /// </summary>
    [MessageLogging(
        EventId = 61006,
        Level = LogLevel.Error,
        Message = "[{serviceTypeName}] Failed to resolve factory '{factoryTypeName}' from DI container")]
    public static partial IGenericMessage FactoryResolutionFailed(
        ILogger logger,
        string serviceTypeName,
        string factoryTypeName);

    /// <summary>
    /// Logs when a ServiceType has no configuration section.
    /// </summary>
    [MessageLogging(
        EventId = 61007,
        Level = LogLevel.Warning,
        Message = "[{serviceTypeName}] No configuration section found at '{sectionName}'")]
    public static partial IGenericMessage ConfigurationSectionNotFound(
        ILogger logger,
        string serviceTypeName,
        string sectionName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Diagnostic Events (8061-8080)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs detailed ServiceType information for debugging.
    /// </summary>
    [MessageLogging(
        EventId = 11027,
        Level = LogLevel.Debug,
        Message = "[{serviceTypeName}] Type details - Section: {sectionName}, Factory: {factoryTypeName}, Category: {category}")]
    public static partial IGenericMessage ServiceTypeDetails(
        ILogger logger,
        string serviceTypeName,
        string sectionName,
        string factoryTypeName,
        string category);

    /// <summary>
    /// Logs when collection state is queried.
    /// </summary>
    [MessageLogging(
        EventId = 11028,
        Level = LogLevel.Debug,
        Message = "[{collectionName}] Collection state - Frozen: {isFrozen}, Count: {count}")]
    public static partial IGenericMessage CollectionState(
        ILogger logger,
        string collectionName,
        bool isFrozen,
        int count);

    /// <summary>
    /// Logs the registration order for debugging.
    /// </summary>
    [MessageLogging(
        EventId = 11029,
        Level = LogLevel.Debug,
        Message = "[{collectionName}] Registration order: {order}")]
    public static partial IGenericMessage RegistrationOrder(
        ILogger logger,
        string collectionName,
        string order);

    // ═══════════════════════════════════════════════════════════════════════════
    // Trace-Level Diagnostic Events (8064-8080)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Traces, in a phase loop, that a single service type option is being invoked by name.
    /// Used by the generated Configure phase to name each option as it is configured (the section
    /// name is not available on the option interface, so this is the name-only per-member line).
    /// </summary>
    [MessageLogging(
        EventId = 11030,
        Level = LogLevel.Trace,
        Message = "[{collectionName}] Configuring type option '{serviceTypeName}'")]
    public static partial IGenericMessage TypeOptionConfiguring(
        ILogger logger,
        string collectionName,
        string serviceTypeName);

    /// <summary>
    /// Traces when starting to configure a single service type option.
    /// </summary>
    [MessageLogging(
        EventId = 11031,
        Level = LogLevel.Trace,
        Message = "[{collectionName}] Configure starting for type option '{serviceTypeName}' (section: {sectionName})")]
    public static partial IGenericMessage TypeOptionConfigureStarting(
        ILogger logger,
        string collectionName,
        string serviceTypeName,
        string sectionName);

    /// <summary>
    /// Traces when configure completes for a single service type option.
    /// </summary>
    [MessageLogging(
        EventId = 11032,
        Level = LogLevel.Trace,
        Message = "[{collectionName}] Configure completed for type option '{serviceTypeName}'")]
    public static partial IGenericMessage TypeOptionConfigureCompleted(
        ILogger logger,
        string collectionName,
        string serviceTypeName);

    /// <summary>
    /// Traces when starting to register services for a single service type option.
    /// </summary>
    [MessageLogging(
        EventId = 11033,
        Level = LogLevel.Trace,
        Message = "[{collectionName}] Register starting for type option '{serviceTypeName}'")]
    public static partial IGenericMessage TypeOptionRegisterStarting(
        ILogger logger,
        string collectionName,
        string serviceTypeName);

    /// <summary>
    /// Traces when register completes for a single service type option.
    /// </summary>
    [MessageLogging(
        EventId = 11034,
        Level = LogLevel.Trace,
        Message = "[{collectionName}] Register completed for type option '{serviceTypeName}'")]
    public static partial IGenericMessage TypeOptionRegisterCompleted(
        ILogger logger,
        string collectionName,
        string serviceTypeName);

    /// <summary>
    /// Traces when starting to register factory for a single service type option.
    /// </summary>
    [MessageLogging(
        EventId = 11035,
        Level = LogLevel.Trace,
        Message = "[{collectionName}] RegisterFactory starting for type option '{serviceTypeName}'")]
    public static partial IGenericMessage TypeOptionRegisterFactoryStarting(
        ILogger logger,
        string collectionName,
        string serviceTypeName);

    /// <summary>
    /// Traces when register factory completes for a single service type option.
    /// </summary>
    [MessageLogging(
        EventId = 11036,
        Level = LogLevel.Trace,
        Message = "[{collectionName}] RegisterFactory completed for type option '{serviceTypeName}'")]
    public static partial IGenericMessage TypeOptionRegisterFactoryCompleted(
        ILogger logger,
        string collectionName,
        string serviceTypeName);

    /// <summary>
    /// Traces detailed configuration binding for a type option.
    /// </summary>
    [MessageLogging(
        EventId = 11039,
        Level = LogLevel.Trace,
        Message = "[{collectionName}] Type option '{serviceTypeName}' binding configuration - Section: '{sectionName}', ConfigType: {configurationType}")]
    public static partial IGenericMessage TypeOptionBindingConfiguration(
        ILogger logger,
        string collectionName,
        string serviceTypeName,
        string sectionName,
        string configurationType);

    /// <summary>
    /// Traces when a type option discovers configuration instances.
    /// </summary>
    [MessageLogging(
        EventId = 11040,
        Level = LogLevel.Trace,
        Message = "[{collectionName}] Type option '{serviceTypeName}' found {count} configuration instance(s)")]
    public static partial IGenericMessage TypeOptionConfigurationInstancesFound(
        ILogger logger,
        string collectionName,
        string serviceTypeName,
        int count);

    /// <summary>
    /// Traces individual configuration instance details.
    /// </summary>
    [MessageLogging(
        EventId = 11041,
        Level = LogLevel.Trace,
        Message = "[{collectionName}] Configuration instance: Name='{configName}', Type='{serviceTypeName}'")]
    public static partial IGenericMessage ConfigurationInstanceDetails(
        ILogger logger,
        string collectionName,
        string configName,
        string serviceTypeName);

    /// <summary>
    /// Traces provider instantiation details.
    /// </summary>
    [MessageLogging(
        EventId = 11042,
        Level = LogLevel.Trace,
        Message = "[{collectionName}] Creating provider '{providerType}'")]
    public static partial IGenericMessage CreatingProvider(
        ILogger logger,
        string collectionName,
        string providerType);

    /// <summary>
    /// Traces provider instantiation completion.
    /// </summary>
    [MessageLogging(
        EventId = 11043,
        Level = LogLevel.Trace,
        Message = "[{collectionName}] Provider '{providerType}' created successfully")]
    public static partial IGenericMessage ProviderCreated(
        ILogger logger,
        string collectionName,
        string providerType);

    /// <summary>
    /// Traces when Initialize resolves provider from DI.
    /// </summary>
    [MessageLogging(
        EventId = 11044,
        Level = LogLevel.Trace,
        Message = "[{collectionName}] Resolving provider '{providerInterface}' from DI")]
    public static partial IGenericMessage ResolvingProviderFromDI(
        ILogger logger,
        string collectionName,
        string providerInterface);

    /// <summary>
    /// Traces collection lookup by name.
    /// </summary>
    [MessageLogging(
        EventId = 11045,
        Level = LogLevel.Trace,
        Message = "[{collectionName}] Looking up type by name: '{typeName}'")]
    public static partial IGenericMessage LookingUpByName(
        ILogger logger,
        string collectionName,
        string typeName);

    /// <summary>
    /// Traces collection lookup by ID.
    /// </summary>
    [MessageLogging(
        EventId = 11046,
        Level = LogLevel.Trace,
        Message = "[{collectionName}] Looking up type by ID: '{typeId}'")]
    public static partial IGenericMessage LookingUpById(
        ILogger logger,
        string collectionName,
        string typeId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Provider Events (8081-8099)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a factory is registered with a provider.
    /// </summary>
    [MessageLogging(
        EventId = 11047,
        Level = LogLevel.Debug,
        Message = "[{domainName}] Factory registered for type '{typeName}'")]
    public static partial IGenericMessage ProviderFactoryRegistered(
        ILogger logger,
        string domainName,
        string typeName);

    /// <summary>
    /// Logs when getting a service by type.
    /// </summary>
    [MessageLogging(
        EventId = 11048,
        Level = LogLevel.Debug,
        Message = "[{domainName}] Getting service for type '{serviceType}'")]
    public static partial IGenericMessage GettingServiceByType(
        ILogger logger,
        string domainName,
        string serviceType);

    /// <summary>
    /// Logs when creating a service with a factory.
    /// </summary>
    [MessageLogging(
        EventId = 11049,
        Level = LogLevel.Debug,
        Message = "[{domainName}] Creating service with factory '{factoryType}'")]
    public static partial IGenericMessage CreatingServiceWithFactory(
        ILogger logger,
        string domainName,
        string factoryType);

    /// <summary>
    /// Logs when getting a service by configuration name.
    /// </summary>
    [MessageLogging(
        EventId = 11050,
        Level = LogLevel.Debug,
        Message = "[{domainName}] Getting service by name: '{configurationName}'")]
    public static partial IGenericMessage GettingServiceByName(
        ILogger logger,
        string domainName,
        string configurationName);

    /// <summary>
    /// Logs when configuration is loaded for a service.
    /// </summary>
    [MessageLogging(
        EventId = 11051,
        Level = LogLevel.Debug,
        Message = "[{domainName}] Configuration loaded for '{configurationName}', type: '{serviceType}'")]
    public static partial IGenericMessage ConfigurationLoaded(
        ILogger logger,
        string domainName,
        string configurationName,
        string serviceType);

    /// <summary>
    /// Logs when user configuration cache is cleared.
    /// </summary>
    [MessageLogging(
        EventId = 11052,
        Level = LogLevel.Debug,
        Message = "[{domainName}] Cleared {count} cached configuration(s)")]
    public static partial IGenericMessage UserCacheCleared(
        ILogger logger,
        string domainName,
        int count);

    /// <summary>
    /// Logs when service cast succeeds.
    /// </summary>
    [MessageLogging(
        EventId = 11053,
        Level = LogLevel.Debug,
        Message = "[{domainName}] Cast succeeded to '{targetType}'")]
    public static partial IGenericMessage CastSucceeded(
        ILogger logger,
        string domainName,
        string targetType);

    /// <summary>
    /// Logs when service cast fails.
    /// </summary>
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Warning,
        Message = "[{domainName}] Cast failed - expected: '{expectedType}', actual: '{actualType}'")]
    public static partial IGenericMessage CastFailed(
        ILogger logger,
        string domainName,
        string expectedType,
        string actualType);

    /// <summary>
    /// Logs when factory registration fails due to null/empty type name.
    /// </summary>
    [MessageLogging(
        EventId = 21002,
        Level = LogLevel.Error,
        Message = "[{domainName}] Factory registration failed: type name is null or empty")]
    public static partial IGenericMessage FactoryRegistrationFailedNullTypeName(
        ILogger logger,
        string domainName);

    /// <summary>
    /// Logs when factory registration fails due to null factory.
    /// </summary>
    [MessageLogging(
        EventId = 21003,
        Level = LogLevel.Error,
        Message = "[{domainName}] Factory registration failed for '{typeName}': factory is null")]
    public static partial IGenericMessage FactoryRegistrationFailedNullFactory(
        ILogger logger,
        string domainName,
        string typeName);

    /// <summary>
    /// Logs when service type is not found in configuration.
    /// </summary>
    [MessageLogging(
        EventId = 61008,
        Level = LogLevel.Error,
        Message = "[{domainName}] Service type is required but was not specified in configuration")]
    public static partial IGenericMessage ServiceTypeRequired(
        ILogger logger,
        string domainName);

    /// <summary>
    /// Logs when no factory is registered for a service type.
    /// </summary>
    [MessageLogging(
        EventId = 61009,
        Level = LogLevel.Error,
        Message = "[{domainName}] No factory registered for type: '{typeName}'")]
    public static partial IGenericMessage NoFactoryRegistered(
        ILogger logger,
        string domainName,
        string typeName);

    /// <summary>
    /// Logs when configuration is not found.
    /// </summary>
    [MessageLogging(
        EventId = 31001,
        Level = LogLevel.Error,
        Message = "[{domainName}] Configuration not found: '{configurationName}'")]
    public static partial IGenericMessage ConfigurationNotFound(
        ILogger logger,
        string domainName,
        string configurationName);

    /// <summary>
    /// Logs when configuration load fails.
    /// </summary>
    [MessageLogging(
        EventId = 61010,
        Level = LogLevel.Error,
        Message = "[{domainName}] Configuration load failed for '{configurationName}' (type: '{typeName}')")]
    public static partial IGenericMessage ConfigurationLoadFailed(
        ILogger logger,
        string domainName,
        string configurationName,
        string typeName);

    /// <summary>
    /// Logs when getting service by name throws an exception.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "[{domainName}] Error getting service by name: '{configurationName}'")]
    public static partial IGenericMessage GetServiceByNameFailed(
        ILogger logger,
        Exception exception,
        string domainName,
        string configurationName);

    /// <summary>
    /// Logs when getting service by ID is not implemented.
    /// </summary>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Warning,
        Message = "[{domainName}] Get by ID not implemented: {configurationId}")]
    public static partial IGenericMessage GetByIdNotImplemented(
        ILogger logger,
        string domainName,
        int configurationId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Phase Invocation Events (11054-11061, 61011-61012)
    //
    // Emitted by the phase invokers on ServiceTypeCollectionBase and ServiceTypeBase.
    // Each phase reports, at Info, whether it is about to run the body the framework
    // supplied or one an application replaced via the gerund setter, and reports the
    // outcome once the body returns. The sequence numbers answer "in what order did
    // this actually run" — the question that is otherwise unanswerable from a log.
    // ═══════════════════════════════════════════════════════════════════════════

    // Why a single const rather than the URL baked into each template: at GA this link moves
    // once, and one place should change. Passing it as a parameter keeps the templates stable.
#pragma warning disable MA0026 // TODO is intentional and tracked — see FDW-638.
    // TODO: FDW-638 — repoint at the published documentation URL when the framework reaches GA.
    // Until then this names the in-repo wiki page, which is the only durable target that exists.
#pragma warning restore MA0026
    /// <summary>
    /// The documentation the phase-invocation messages point at, so a reader who sees
    /// "custom implementation" in a log has somewhere to go to find out what that means.
    /// </summary>
    public const string PhaseDocumentation = "wiki/10-TypeCollection-Patterns.md";

    /// <summary>
    /// Logs that a collection phase is about to run the framework-supplied body.
    /// </summary>
    [MessageLogging(
        EventId = 11054,
        Level = LogLevel.Information,
        Message = "[{collectionName}] {phase} (collection #{sequence}) running the DEFAULT implementation — see {documentation}")]
    public static partial IGenericMessage CollectionPhaseDefault(
        ILogger logger,
        string collectionName,
        string phase,
        int sequence,
        string documentation);

    /// <summary>
    /// Logs that a collection phase is about to run a body an application supplied.
    /// </summary>
    [MessageLogging(
        EventId = 11055,
        Level = LogLevel.Information,
        Message = "[{collectionName}] {phase} (collection #{sequence}) running a CUSTOM implementation — see {documentation}")]
    public static partial IGenericMessage CollectionPhaseCustom(
        ILogger logger,
        string collectionName,
        string phase,
        int sequence,
        string documentation);

    /// <summary>
    /// Logs that a collection phase completed.
    /// </summary>
    [MessageLogging(
        EventId = 11056,
        Level = LogLevel.Information,
        Message = "[{collectionName}] {phase} (collection #{sequence}) completed successfully over {optionCount} option(s)")]
    public static partial IGenericMessage CollectionPhaseSucceeded(
        ILogger logger,
        string collectionName,
        string phase,
        int sequence,
        int optionCount);

    /// <summary>
    /// Logs that an option phase is about to run the framework-supplied body.
    /// </summary>
    [MessageLogging(
        EventId = 11057,
        Level = LogLevel.Information,
        Message = "[{optionName}] {phase} (option #{ordinal} in {collectionName}) running the DEFAULT implementation — see {documentation}")]
    public static partial IGenericMessage OptionPhaseDefault(
        ILogger logger,
        string optionName,
        string phase,
        int ordinal,
        string collectionName,
        string documentation);

    /// <summary>
    /// Logs that an option phase is about to run a body the option or host supplied.
    /// </summary>
    [MessageLogging(
        EventId = 11058,
        Level = LogLevel.Information,
        Message = "[{optionName}] {phase} (option #{ordinal} in {collectionName}) running a CUSTOM implementation — see {documentation}")]
    public static partial IGenericMessage OptionPhaseCustom(
        ILogger logger,
        string optionName,
        string phase,
        int ordinal,
        string collectionName,
        string documentation);

    /// <summary>
    /// Logs that an option phase completed.
    /// </summary>
    [MessageLogging(
        EventId = 11059,
        Level = LogLevel.Information,
        Message = "[{optionName}] {phase} (option #{ordinal} in {collectionName}) completed successfully")]
    public static partial IGenericMessage OptionPhaseSucceeded(
        ILogger logger,
        string optionName,
        string phase,
        int ordinal,
        string collectionName);

    /// <summary>
    /// Logs that a collection phase threw. The exception is converted to a failure result after this
    /// is logged — the caller decides whether a half-registered domain may reach a running application.
    /// </summary>
    [MessageLogging(
        EventId = 61011,
        Level = LogLevel.Error,
        Message = "[{collectionName}] {phase} (collection #{sequence}) FAILED while running the {implementation} implementation")]
    public static partial IGenericMessage CollectionPhaseFailed(
        ILogger logger,
        Exception exception,
        string collectionName,
        string phase,
        int sequence,
        string implementation);

    /// <summary>
    /// Logs that an option phase threw. The exception is converted to a failure result after this is
    /// logged, so one throwing option cannot unwind a sweep that is handling failures as values.
    /// </summary>
    [MessageLogging(
        EventId = 61012,
        Level = LogLevel.Error,
        Message = "[{optionName}] {phase} (option #{ordinal} in {collectionName}) FAILED while running the {implementation} implementation")]
    public static partial IGenericMessage OptionPhaseFailed(
        ILogger logger,
        Exception exception,
        string optionName,
        string phase,
        int ordinal,
        string collectionName,
        string implementation);

    // Why these two are separate from the *Failed pair above: a phase that RETURNS a failure did so
    // deliberately and carries its own domain's code and message, whereas a phase that THREW did not
    // choose to fail and has only an exception. Logging both as "FAILED ... while running" would make
    // a deliberate, well-described refusal indistinguishable from a crash in the one place someone
    // reads to tell them apart. No exception parameter here, because there is no exception.

    /// <summary>
    /// Logs that an option's phase body returned a failure result rather than throwing.
    /// </summary>
    [MessageLogging(
        EventId = 61013,
        Level = LogLevel.Error,
        Message = "[{optionName}] {phase} (option #{ordinal} in {collectionName}) returned a failure: {reason}")]
    public static partial IGenericMessage OptionPhaseReportedFailure(
        ILogger logger,
        string optionName,
        string phase,
        int ordinal,
        string collectionName,
        string reason);

    /// <summary>
    /// Logs that a collection's sweep stopped because one of its options returned a failure.
    /// </summary>
    [MessageLogging(
        EventId = 61014,
        Level = LogLevel.Error,
        Message = "[{collectionName}] {phase} stopped at option '{optionName}', which returned a failure: {reason}")]
    public static partial IGenericMessage CollectionPhaseStopped(
        ILogger logger,
        string collectionName,
        string phase,
        string optionName,
        string reason);

    /// <summary>
    /// Logged when a command reaches a service type that declares no service to run it.
    /// </summary>
    /// <remarks>
    /// Warning rather than Error: nothing is broken at this point, and the caller still gets a
    /// failure result to act on. What it records is that something resolved a service type whose
    /// whole job is to register during the three phases, expecting a service that was never meant
    /// to exist — a wiring mistake, and one that would otherwise surface far from its cause.
    /// </remarks>
    [MessageLogging(
        EventId = 61015,
        Level = LogLevel.Warning,
        Message = "A {commandType} command was dispatched to {serviceTypeName}, which declares no service to run it")]
    public static partial IGenericMessage NoServiceToExecute(
        ILogger logger,
        string commandType,
        string serviceTypeName);
}
