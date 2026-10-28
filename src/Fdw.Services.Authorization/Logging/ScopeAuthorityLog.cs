using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Logging;

/// <summary>
/// MessageLogging for the scope check.
/// EventId ranges: 12780-12781 (allowed), 22780 (no scope named, 400), 52780 (not signed in, 401), 102780-102782 (signed in but not allowed, 403).
/// </summary>
[MessageLoggingTypeCode("AUTHORIZATION")]
public static partial class ScopeAuthorityLog
{
    /// <summary>Logs that a caller was allowed to act at a scope.</summary>
    [MessageLogging(EventId = 12780, Level = LogLevel.Information, Message = "Allowed: the caller holds '{right}' at {scope} or above")]
    public static partial IGenericMessage Allowed(ILogger logger, string right, string scope);

    /// <summary>Logs that a platform administrator was allowed to act at a scope without holding a role there (FDW-2852).</summary>
    [MessageLogging(EventId = 12781, Level = LogLevel.Information, Message = "Allowed: the caller administers the platform, so '{right}' holds at {scope}")]
    public static partial IGenericMessage AllowedPlatformAdministrator(ILogger logger, string right, string scope);

    /// <summary>Logs that a check was refused because the thing named no tenant.</summary>
    [MessageLogging(EventId = 22780, Level = LogLevel.Warning, Message = "Refused '{right}': the thing names no tenant, so there is no scope to judge it at")]
    public static partial IGenericMessage NoScope(ILogger logger, string right);

    /// <summary>Logs that a check was refused because nobody is signed in.</summary>
    [MessageLogging(EventId = 52780, Level = LogLevel.Warning, Message = "Refused '{right}' at {scope}: the caller is not signed in")]
    public static partial IGenericMessage CallerRequired(ILogger logger, string right, string scope);

    /// <summary>Logs that a check was refused because the thing is in a tenant the caller does not act in.</summary>
    [MessageLogging(EventId = 102780, Level = LogLevel.Warning, Message = "Refused '{right}' at {scope}: the caller does not act in that tenant")]
    public static partial IGenericMessage OtherTenant(ILogger logger, string right, string scope);

    /// <summary>Logs that a check was refused because the caller's session does not hold the right.</summary>
    [MessageLogging(EventId = 102781, Level = LogLevel.Warning, Message = "Refused '{right}' at {scope}: the caller's session does not hold it")]
    public static partial IGenericMessage SessionLacksRight(ILogger logger, string right, string scope);

    /// <summary>Logs that a check was refused because the caller holds the right at no scope that reaches the thing.</summary>
    [MessageLogging(EventId = 102782, Level = LogLevel.Warning, Message = "Refused '{right}' at {scope}: the caller holds it at no scope that reaches the thing")]
    public static partial IGenericMessage RightNotHeldAtScope(ILogger logger, string right, string scope);
}
