using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Services.Authentication.Steps;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for the steps shipped with the platform.
/// </summary>
/// <remarks>
/// EventId range: 91140–91145. A subject identifier is never logged: it identifies a person at an
/// external authority, and an issuer plus a local principal id correlate as well without doing so.
/// </remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class StepLog
{
    /// <summary>An authenticated subject has no binding to any local principal.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority that asserted them.</param>
    // Why Warning: someone authenticated successfully somewhere we trust and is still unknown here.
    // That is provisioning policy declining, or a real person hitting the wrong tenant — expected,
    // handled, and worth seeing without being a defect.
    [MessageLogging(EventId = 91140, Level = LogLevel.Warning,
        Message = "A subject asserted by '{issuer}' is bound to no local principal")]
    internal static partial IGenericMessage NoBinding(ILogger<ResolvePrincipalStep> logger, string issuer);

    /// <summary>A subject was resolved to a local principal.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority that asserted them.</param>
    /// <param name="principalId">The local principal.</param>
    [MessageLogging(EventId = 91141, Level = LogLevel.Trace,
        Message = "Subject asserted by '{issuer}' resolved to principal {principalId}")]
    internal static partial IGenericMessage PrincipalResolved(
        ILogger<ResolvePrincipalStep> logger, string issuer, Guid principalId);

    /// <summary>Eligibility for issuance was decided.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="principalId">The principal.</param>
    /// <param name="permitted">Whether issuance is permitted.</param>
    /// <param name="reason">Why.</param>
    // Why Trace and not Warning on a denial: the runner's terminal check logs the refusal at
    // Warning with the same reason. Logging it twice at that level would double-count every denial
    // in whatever counts them.
    [MessageLogging(EventId = 91142, Level = LogLevel.Trace,
        Message = "Principal {principalId} issuance permitted={permitted}: {reason}")]
    internal static partial IGenericMessage EligibilityDecided(
        ILogger<AuthorizeIssuanceStep> logger, Guid principalId, bool permitted, string reason);
}
