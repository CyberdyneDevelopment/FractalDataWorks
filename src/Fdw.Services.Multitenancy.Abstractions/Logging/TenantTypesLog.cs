using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Abstractions.Logging;

/// <summary>
/// MessageLogging for TenantTypes initialization and registration operations.
/// EventId range: 4410-4429
/// </summary>
[MessageLoggingTypeCode("ABSTRACTIONS8")]
public static partial class TenantTypesLog
{
    /// <summary>
    /// Logs when Tenant IOptions bindings are configured.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Debug,
        Message = "Configured Tenant IOptions bindings")]
    public static partial IGenericMessage ConfiguredOptionsBindings(
        ILogger logger);

    /// <summary>
    /// Logs when Tenant infrastructure services are registered.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Debug,
        Message = "Registered Tenant infrastructure services")]
    public static partial IGenericMessage RegisteredInfrastructureServices(
        ILogger logger);

    /// <summary>
    /// Logs when a Tenant configuration entry is skipped due to empty Name.
    /// </summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Warning,
        Message = "Skipping Tenant configuration '{slug}' with empty Name")]
    public static partial IGenericMessage SkippingEmptyName(
        ILogger logger,
        string slug);

    /// <summary>
    /// Logs when a Tenant is already registered, skipping configuration.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Debug,
        Message = "Tenant '{slug}' already registered, skipping configuration")]
    public static partial IGenericMessage TenantAlreadyRegistered(
        ILogger logger,
        string slug);

    /// <summary>
    /// Logs when a configured Tenant is registered at runtime.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "Registered configured Tenant '{name}' ({slug})")]
    public static partial IGenericMessage RegisteredConfiguredTenant(
        ILogger logger,
        string name,
        string slug);

    /// <summary>
    /// Logs summary of Tenant initialization with both compile-time and configured counts.
    /// </summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Information,
        Message = "Initialized {compileTimeCount} compile-time Tenants and {configCount} configured Tenants")]
    public static partial IGenericMessage InitializedWithConfigured(
        ILogger logger,
        int compileTimeCount,
        int configCount);

    /// <summary>
    /// Logs summary of Tenant initialization with compile-time types only.
    /// </summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Information,
        Message = "Initialized {compileTimeCount} Tenant types")]
    public static partial IGenericMessage Initialized(
        ILogger logger,
        int compileTimeCount);

    /// <summary>
    /// Returned by <see cref="Fdw.Services.Multitenancy.Abstractions.NullOrganizationProvider"/>
    /// when org lookup is attempted but multitenancy is not enabled.
    /// </summary>
    // Why: EventId 4417 — next available in the TenantTypesLog range (4410-4429).
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Debug,
        Message = "Organization lookup not available — multitenancy is not enabled")]
    public static partial IGenericMessage OrgLookupUnavailable(ILogger logger);

    /// <summary>
    /// Returned by <see cref="Fdw.Services.Multitenancy.Abstractions.NullTenantProvider"/> when
    /// tenant lookup is attempted but this host runs the SingleTenant multitenancy option.
    /// </summary>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Debug,
        Message = "Tenant lookup not available — this host runs the SingleTenant multitenancy option")]
    public static partial IGenericMessage TenantLookupUnavailable(ILogger logger);
}
