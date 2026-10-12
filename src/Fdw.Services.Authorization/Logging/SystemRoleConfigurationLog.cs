using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Logging;

/// <summary>
/// MessageLogging for <c>DefaultSystemRoleConfiguration</c> operations.
/// EventId range: 3144-3149
/// </summary>
[MessageLoggingTypeCode("AUTHORIZATION")]
public static partial class SystemRoleConfigurationLog
{
    /// <summary>Logs when AdminRoleName is absent or empty — startup fatal.</summary>
    [MessageLogging(EventId = 61001, Level = LogLevel.Critical,
        Message = "authz:SystemRoleMapping:AdminRoleName is required but is missing or empty. Application cannot start.")]
    public static partial IGenericMessage AdminRoleNameMissing(ILogger logger);

    /// <summary>Logs successful initialization with the configured role names.</summary>
    [MessageLogging(EventId = 11017, Level = LogLevel.Information,
        Message = "SystemRoleConfiguration initialized: adminRole='{adminRoleName}', operatorRole='{operatorRoleName}', viewerRole='{viewerRoleName}'")]
    public static partial IGenericMessage Initialized(ILogger logger, string adminRoleName, string? operatorRoleName, string? viewerRoleName);
}
