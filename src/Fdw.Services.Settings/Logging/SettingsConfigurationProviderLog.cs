using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Logging;

/// <summary>
/// MessageLogging for SettingsConfigurationProvider operations.
/// EventId range: 9410-9419
/// </summary>
[MessageLoggingTypeCode("SETTINGS")]
public static partial class SettingsConfigurationProviderLog
{
    /// <summary>
    /// Logs that server settings were retrieved, reporting the count.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="count">The number of server settings that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "Getting server settings, count: {count}")]
    public static partial IGenericMessage ServerSettingsLoaded(ILogger logger, int count);

    /// <summary>
    /// Logs that tenant settings were retrieved, reporting the count.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="count">The number of tenant settings that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "Getting tenant settings, count: {count}")]
    public static partial IGenericMessage TenantSettingsLoaded(ILogger logger, int count);

    /// <summary>
    /// Logs that role settings were retrieved, reporting the count.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="count">The number of role settings that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "Getting role settings, count: {count}")]
    public static partial IGenericMessage RoleSettingsLoaded(ILogger logger, int count);

    /// <summary>
    /// Logs that the settings provider was initialized, reporting the server, tenant, and role setting counts.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="serverCount">The number of server settings loaded at initialization.</param>
    /// <param name="tenantCount">The number of tenant settings loaded at initialization.</param>
    /// <param name="roleCount">The number of role settings loaded at initialization.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug,
        Message = "Settings provider initialized with server={serverCount}, tenant={tenantCount}, role={roleCount}")]
    public static partial IGenericMessage ProviderInitialized(ILogger logger, int serverCount, int tenantCount, int roleCount);
}
