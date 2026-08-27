using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Services.Authentication.Steps;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for deciding whether a principal may hold a token.
/// </summary>
/// <remarks>EventId range: 91200–91203.</remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class EligibilityLog
{
    /// <summary>The account is in a state that permits issuance.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="principalId">The principal.</param>
    [MessageLogging(EventId = 91200, Level = LogLevel.Trace,
        Message = "Principal {principalId} may be issued a token")]
    internal static partial IGenericMessage Permitted(
        ILogger<UserAccountEligibility> logger, Guid principalId);

    /// <summary>Issuance was refused.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="principalId">The principal.</param>
    /// <param name="reason">Why.</param>
    // Why Information and not Warning: a disabled account being turned away is the system doing its
    // job, and at Warning a routine departure would look like an incident. The runner logs the
    // resulting refusal at Warning once, which is the count worth having.
    [MessageLogging(EventId = 91201, Level = LogLevel.Information,
        Message = "Principal {principalId} was refused a token: {reason}")]
    internal static partial IGenericMessage Denied(
        ILogger<UserAccountEligibility> logger, Guid principalId, string reason);

    /// <summary>No principal was supplied to decide about.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91202, Level = LogLevel.Error,
        Message = "A principal must be supplied to decide eligibility")]
    internal static partial IGenericMessage PrincipalMissing(ILogger<UserAccountEligibility> logger);
}
