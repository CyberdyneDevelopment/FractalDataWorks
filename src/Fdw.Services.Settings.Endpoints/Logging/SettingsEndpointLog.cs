using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints.Logging;

/// <summary>
/// High-performance MessageLogging for settings endpoint operations.
/// EventId range: 7020-7049
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS12")]
public static partial class SettingsEndpointLog
{
    /// <summary>Logs when listing server settings.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "Listing server settings")]
    public static partial IGenericMessage ListingServerSettings(ILogger logger);

    /// <summary>Logs when getting a server setting by name.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "Getting server setting '{settingName}'")]
    public static partial IGenericMessage GettingServerSetting(ILogger logger, string settingName);

    /// <summary>Logs when a server setting is created.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information,
        Message = "Created server setting '{settingName}'")]
    public static partial IGenericMessage CreatedServerSetting(ILogger logger, string settingName);

    /// <summary>Logs when a server setting is updated.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Information,
        Message = "Updated server setting '{settingName}'")]
    public static partial IGenericMessage UpdatedServerSetting(ILogger logger, string settingName);

    /// <summary>Logs when listing tenant settings.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace,
        Message = "Listing tenant settings for tenant '{tenantId}'")]
    public static partial IGenericMessage ListingTenantSettings(ILogger logger, string tenantId);

    /// <summary>Logs when a tenant setting is created.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Information,
        Message = "Created tenant setting '{settingName}' for tenant '{tenantId}'")]
    public static partial IGenericMessage CreatedTenantSetting(ILogger logger, string settingName, string tenantId);

    /// <summary>Logs when a tenant setting is updated.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Information,
        Message = "Updated tenant setting '{settingName}' for tenant '{tenantId}'")]
    public static partial IGenericMessage UpdatedTenantSetting(ILogger logger, string settingName, string tenantId);

    /// <summary>Logs when listing role settings.</summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Trace,
        Message = "Listing role settings for role '{roleName}' in tenant '{tenantId}'")]
    public static partial IGenericMessage ListingRoleSettings(ILogger logger, string roleName, string tenantId);

    /// <summary>Logs when a role setting is created.</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Information,
        Message = "Created role setting '{settingName}' for role '{roleName}' in tenant '{tenantId}'")]
    public static partial IGenericMessage CreatedRoleSetting(ILogger logger, string settingName, string roleName, string tenantId);

    /// <summary>Logs when a role setting is updated.</summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Information,
        Message = "Updated role setting '{settingName}' for role '{roleName}' in tenant '{tenantId}'")]
    public static partial IGenericMessage UpdatedRoleSetting(ILogger logger, string settingName, string roleName, string tenantId);

    /// <summary>Logs when a server setting is not found.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning,
        Message = "Server setting '{settingName}' not found")]
    public static partial IGenericMessage ServerSettingNotFound(ILogger logger, string settingName);

    /// <summary>Logs when a tenant setting is not found.</summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Warning,
        Message = "Tenant setting '{settingName}' not found for tenant '{tenantId}'")]
    public static partial IGenericMessage TenantSettingNotFound(ILogger logger, string settingName, string tenantId);

    /// <summary>Logs when a role setting is not found.</summary>
    [MessageLogging(EventId = 31002, Level = LogLevel.Warning,
        Message = "Role setting '{settingName}' not found for role '{roleName}' in tenant '{tenantId}'")]
    public static partial IGenericMessage RoleSettingNotFound(ILogger logger, string settingName, string roleName, string tenantId);

    /// <summary>Logs when a server setting already exists.</summary>
    [MessageLogging(EventId = 41000, Level = LogLevel.Warning,
        Message = "Server setting '{settingName}' already exists")]
    public static partial IGenericMessage ServerSettingAlreadyExists(ILogger logger, string settingName);
}
