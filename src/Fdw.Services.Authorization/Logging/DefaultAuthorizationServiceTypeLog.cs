using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Logging;

/// <summary>
/// MessageLogging for <see cref="Fdw.Services.Authorization.DefaultAuthorizationServiceType"/>'s
/// registration.
/// </summary>
/// <remarks>
/// EventId range: 4150–4152. Registration here is a single, deliberately long sequential DI
/// registrant (<c>[ConventionOverride(MaxMethodLines = 80)]</c>, <c>#pragma warning disable
/// FDW006</c>) rather than per-option dispatch, so it does not get the standard invoker's
/// registration logging for free -- see <see cref="DefaultAuthorizationServiceType"/>'s own remarks.
/// </remarks>
[MessageLoggingTypeCode("AUTHORIZATION")]
internal static partial class DefaultAuthorizationServiceTypeLog
{
    /// <summary>Authorization services (resolver, ASP.NET Core bridge, domain providers) were registered.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 4150, Level = LogLevel.Trace,
        Message = "Registered IFrameworkAuthorizationService, IUserPermissions, the ASP.NET Core authorization bridge, and the Role/Permission/RolePermission/UserRole/TenantOrgAccess domain providers")]
    internal static partial IGenericMessage Registered(ILogger logger);

    /// <summary>No SystemRoleMapping row was found, so which role names carry system authority is unknown.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="reason">What the read reported.</param>
    // Critical: the host is starting and cannot determine system authority for any principal --
    // thrown immediately after, since this runs inside a DI factory that cannot return an
    // IGenericResult, but it still needs a structured record ahead of the exception.
    [MessageLogging(EventId = 4151, Level = LogLevel.Critical,
        Message = "SystemRoleMapping is not configured in the authorization store: {reason}")]
    internal static partial IGenericMessage SystemRoleMappingMissing(ILogger logger, string? reason);

    /// <summary>The RoleMapping row named an implementation other than System.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="implementation">The implementation the row actually named.</param>
    [MessageLogging(EventId = 4152, Level = LogLevel.Critical,
        Message = "RoleMapping row named implementation '{implementation}', not System -- authorization cannot start without knowing which role names carry system authority")]
    internal static partial IGenericMessage SystemRoleMappingWrongImplementation(ILogger logger, string implementation);
}
