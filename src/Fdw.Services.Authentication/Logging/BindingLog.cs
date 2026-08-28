using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Services.Authentication.Binding;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for resolving an external subject to a local principal.
/// </summary>
/// <remarks>
/// EventId range: 91190–91194. The external subject never appears — it identifies a person at
/// another authority, and the issuer plus the local user id say which trust relationship and which
/// account without naming them there.
/// </remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class BindingLog
{
    /// <summary>An external subject resolved to a local user.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority that asserted them.</param>
    /// <param name="userId">The local user.</param>
    [MessageLogging(EventId = 91190, Level = LogLevel.Trace,
        Message = "A subject from '{issuer}' is bound to user {userId}")]
    internal static partial IGenericMessage Bound(
        ILogger<ExternalIdentityBinding> logger, string issuer, Guid userId);

    /// <summary>No binding exists for an authenticated subject.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority that asserted them.</param>
    [MessageLogging(EventId = 91191, Level = LogLevel.Debug,
        Message = "A subject from '{issuer}' has no active binding")]
    internal static partial IGenericMessage Unbound(
        ILogger<ExternalIdentityBinding> logger, string issuer);

    /// <summary>One external subject is bound to more than one user.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority that asserted them.</param>
    /// <param name="count">How many rows matched.</param>
    [MessageLogging(EventId = 91192, Level = LogLevel.Error,
        Message = "A subject from '{issuer}' is bound to {count} users; refusing rather than choosing")]
    internal static partial IGenericMessage Ambiguous(
        ILogger<ExternalIdentityBinding> logger, string issuer, int count);

    /// <summary>A user's tenant could not be determined.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user.</param>
    [MessageLogging(EventId = 91194, Level = LogLevel.Error,
        Message = "User {userId} has no tenant; refusing rather than assuming one")]
    internal static partial IGenericMessage TenantUnknown(
        ILogger<UserTenantResolver> logger, Guid userId);

    /// <summary>The lookup was asked for without both halves of the key.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91193, Level = LogLevel.Error,
        Message = "Both an issuer and a subject are required to resolve a binding")]
    internal static partial IGenericMessage LookupIncomplete(ILogger<ExternalIdentityBinding> logger);
}
