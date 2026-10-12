using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Services.Authentication.Steps;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for proving a caller by password.
/// </summary>
/// <remarks>
/// EventId range: 91240–91242. No password appears at any level, and no message distinguishes an
/// unknown user from a wrong password — the two are one refusal on purpose, so the log cannot be
/// read as an account enumeration oracle either.
/// </remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class PasswordCredentialLog
{
    /// <summary>A caller proved themselves by password.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="principalId">Who they turned out to be.</param>
    [MessageLogging(EventId = 91240, Level = LogLevel.Trace,
        Message = "Password accepted for principal {principalId}")]
    internal static partial IGenericMessage Proved(
        ILogger logger, Guid principalId);

    /// <summary>The credential was refused.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="username">What was presented as a username.</param>
    // Why Information and not Warning: a mistyped password is the single most common event a login
    // endpoint sees. At Warning the ordinary case drowns the genuine ones, and the count that
    // matters - repeated failures against one account - is the credential service's to raise, since
    // it is the only thing that can see the attempt history.
    [MessageLogging(EventId = 91241, Level = LogLevel.Information,
        Message = "Refused the credential presented for '{username}'")]
    internal static partial IGenericMessage Refused(
        ILogger logger, string username);

    /// <summary>The caller presented no credential to check.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91242, Level = LogLevel.Information,
        Message = "A username and password are required to prove a caller by password")]
    internal static partial IGenericMessage NothingPresented(ILogger logger);

    /// <summary>Logs a step asked to run before its Initialize captured what it needs.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stepName">The step the flow named.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 91243, Level = LogLevel.Error,
        Message = "Step '{stepName}' ran before its dependencies were captured, so it cannot do its work. The option's Initialize phase did not run, which means the host was not fully initialized before a flow reached this step")]
    internal static partial IGenericMessage NotInitialized(ILogger logger, string stepName);
}
