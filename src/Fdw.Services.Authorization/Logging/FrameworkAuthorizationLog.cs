using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Logging;

/// <summary>
/// MessageLogging for the ASP.NET Core authorization bridge -- <c>FrameworkPermissionHandler</c> and
/// <c>FdwAuthorizationPolicyProvider</c>.
/// </summary>
/// <remarks>
/// EventId range: 4140–4146. <see cref="Fdw.Services.Authorization.DefaultAuthorizationService"/>
/// and <see cref="Fdw.Services.Authorization.UserPermissions"/> already log the
/// permission RESOLUTION itself; this covers the ASP.NET Core-facing layer around it, which
/// previously logged nothing at all -- an actual exception from Authorize() was indistinguishable
/// from a clean denial, since HandleRequirementAsync just returned either way.
/// </remarks>
[MessageLoggingTypeCode("AUTHORIZATION")]
internal static partial class FrameworkAuthorizationLog
{
    /// <summary>A permission requirement is being checked for an authenticated principal.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="resource">The resource named by the policy.</param>
    /// <param name="action">The action named by the policy.</param>
    [MessageLogging(EventId = 4140, Level = LogLevel.Trace,
        Message = "Checking permission requirement '{resource}:{action}'")]
    internal static partial IGenericMessage CheckingRequirement(ILogger logger, string resource, string action);

    /// <summary>The requirement was skipped because the caller is not authenticated.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="resource">The resource named by the policy.</param>
    /// <param name="action">The action named by the policy.</param>
    [MessageLogging(EventId = 4141, Level = LogLevel.Debug,
        Message = "Requirement '{resource}:{action}' not evaluated -- caller is not authenticated")]
    internal static partial IGenericMessage RequirementSkippedUnauthenticated(
        ILogger logger, string resource, string action);

    /// <summary>The requirement was met.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="resource">The resource named by the policy.</param>
    /// <param name="action">The action named by the policy.</param>
    [MessageLogging(EventId = 4142, Level = LogLevel.Trace,
        Message = "Requirement '{resource}:{action}' satisfied")]
    internal static partial IGenericMessage RequirementSatisfied(ILogger logger, string resource, string action);

    /// <summary>
    /// The underlying authorization check itself failed (an error, not a clean denial) -- distinct
    /// from <see cref="Fdw.Services.Authorization.DefaultAuthorizationService"/>'s own "denied" log,
    /// which only fires when the check ran and returned false.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="resource">The resource named by the policy.</param>
    /// <param name="action">The action named by the policy.</param>
    /// <param name="reason">What the underlying check reported.</param>
    [MessageLogging(EventId = 4143, Level = LogLevel.Error,
        Message = "Authorization check for '{resource}:{action}' failed: {reason}")]
    internal static partial IGenericMessage RequirementCheckFailed(
        ILogger logger, string resource, string action, string? reason);

    /// <summary>A policy name resolved to a permission requirement.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="policyName">The requested policy name.</param>
    /// <param name="resource">The resource it was parsed into.</param>
    /// <param name="action">The action it was parsed into.</param>
    [MessageLogging(EventId = 4144, Level = LogLevel.Trace,
        Message = "Policy '{policyName}' resolved to permission requirement '{resource}:{action}'")]
    internal static partial IGenericMessage PolicyResolvedToPermission(
        ILogger logger, string policyName, string resource, string action);

    /// <summary>A policy name resolved to the configured system-admin role.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="policyName">The requested policy name.</param>
    /// <param name="adminRoleName">The role it was resolved to require.</param>
    [MessageLogging(EventId = 4145, Level = LogLevel.Trace,
        Message = "Policy '{policyName}' resolved to require role '{adminRoleName}'")]
    internal static partial IGenericMessage PolicyResolvedToAdminRole(
        ILogger logger, string policyName, string adminRoleName);

    /// <summary>A policy name did not match any FDW shape and fell through to the ASP.NET Core default provider.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="policyName">The requested policy name.</param>
    [MessageLogging(EventId = 4146, Level = LogLevel.Trace,
        Message = "Policy '{policyName}' did not match any FDW policy shape -- deferring to the default provider")]
    internal static partial IGenericMessage PolicyFellThroughToDefault(ILogger logger, string policyName);
}
