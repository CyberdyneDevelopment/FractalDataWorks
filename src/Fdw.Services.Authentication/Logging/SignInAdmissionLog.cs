using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Services.Authentication.Flow;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for deciding whether a user could sign in by a method.
/// </summary>
/// <remarks>EventId range: 91280–91289.</remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class SignInAdmissionLog
{
    /// <summary>No method was named.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91280, Level = LogLevel.Error,
        Message = "A sign-in method must be named to ask whether a user may use it")]
    internal static partial IGenericMessage MethodMissing(ILogger<SignInMethodAdmission> logger);

    /// <summary>No enabled flow proves the method, so nobody can sign in by it.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="method">The method.</param>
    // Warning: a credential for this method would be unusable by anyone, which is worth an operator's
    // attention -- either the flow is switched off on purpose or something is missing.
    [MessageLogging(EventId = 91281, Level = LogLevel.Warning,
        Message = "No enabled flow proves '{method}', so no user can sign in by it")]
    internal static partial IGenericMessage NoFlowProvesMethod(ILogger<SignInMethodAdmission> logger, string method);

    /// <summary>A flow with no requirement admits the user.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="who">The user, or which account-to-be.</param>
    /// <param name="method">The method.</param>
    /// <param name="flowName">The admitting flow.</param>
    [MessageLogging(EventId = 91282, Level = LogLevel.Trace,
        Message = "{who} may sign in by '{method}': flow '{flowName}' admits every active user")]
    internal static partial IGenericMessage AdmittedByOpenFlow(
        ILogger<SignInMethodAdmission> logger, string who, string method, string flowName);

    /// <summary>A gated flow admits the user.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="who">The user, or which account-to-be.</param>
    /// <param name="method">The method.</param>
    /// <param name="flowName">The admitting flow.</param>
    [MessageLogging(EventId = 91283, Level = LogLevel.Trace,
        Message = "{who} may sign in by '{method}': they hold everything flow '{flowName}' requires")]
    internal static partial IGenericMessage AdmittedByGatedFlow(
        ILogger<SignInMethodAdmission> logger, string who, string method, string flowName);

    /// <summary>Every flow proving the method is gated, and the user holds none of the gates.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="who">The user, or which account-to-be.</param>
    /// <param name="method">The method.</param>
    /// <param name="flows">The flows and what each requires.</param>
    [MessageLogging(EventId = 91284, Level = LogLevel.Information,
        Message = "{who} may not sign in by '{method}': they lack what every flow requires ({flows})")]
    internal static partial IGenericMessage NotAdmitted(
        ILogger<SignInMethodAdmission> logger, string who, string method, string flows);

    /// <summary>The permission resolver answered with nothing.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user.</param>
    [MessageLogging(EventId = 91285, Level = LogLevel.Error,
        Message = "The permissions of user {userId} resolved to nothing; admission cannot be decided")]
    internal static partial IGenericMessage PermissionsUnresolved(ILogger<SignInMethodAdmission> logger, Guid userId);
}
