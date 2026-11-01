using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Logging;

/// <summary>
/// MessageLogging for the grant rule.
/// EventId ranges: 12710 (allowed), 22700 (nothing named, 400), 52700 (not signed in, 401), 102700-102702 (signed in but not allowed, 403).
/// </summary>
[MessageLoggingTypeCode("AUTHORIZATION")]
public static partial class GrantRuleLog
{
    /// <summary>
    /// Logs that a role change was allowed.
    /// </summary>
    [MessageLogging(EventId = 12710, Level = LogLevel.Information, Message = "Role change allowed in tenant '{tenantId}': the caller holds every permission of '{roles}'")]
    public static partial IGenericMessage Allowed(ILogger logger, string tenantId, string roles);

    /// <summary>
    /// Logs that a role change was refused because nobody is signed in.
    /// </summary>
    [MessageLogging(EventId = 52700, Level = LogLevel.Warning, Message = "A role change was refused: the caller is not signed in")]
    public static partial IGenericMessage CallerRequired(ILogger logger);

    /// <summary>
    /// Logs that a role change was refused because it is for a tenant the caller does not act in.
    /// </summary>
    [MessageLogging(EventId = 102700, Level = LogLevel.Warning, Message = "A role change was refused: it is for tenant '{tenantId}', which the caller does not act in")]
    public static partial IGenericMessage OtherTenant(ILogger logger, string tenantId);

    /// <summary>
    /// Logs that a role change was refused because the caller does not hold what the role carries.
    /// </summary>
    [MessageLogging(EventId = 102701, Level = LogLevel.Warning, Message = "A role change was refused: the caller does not hold {permissions}, which the role carries")]
    public static partial IGenericMessage RightsNotHeld(ILogger logger, string permissions);

    /// <summary>
    /// Logs that a role change was refused because the caller holds no role that carries the right to modify what the role carries.
    /// </summary>
    [MessageLogging(EventId = 102702, Level = LogLevel.Warning, Message = "A role change was refused: the caller holds no role that carries the right to modify it, which needs {rights}")]
    public static partial IGenericMessage ModifyRightNotHeld(ILogger logger, string rights);

    /// <summary>
    /// Logs that a role change was refused because no role or permission was named.
    /// </summary>
    [MessageLogging(EventId = 22700, Level = LogLevel.Warning, Message = "A role change was refused: no role or permission was named")]
    public static partial IGenericMessage NothingNamed(ILogger logger);
}
