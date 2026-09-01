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
    [MessageLogging(EventId = 91140, Level = LogLevel.Warning,
        Message = "A subject asserted by '{issuer}' is bound to no local principal")]
    internal static partial IGenericMessage NoBinding(ILogger logger, string issuer);

    /// <summary>A subject was resolved to a local principal.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority that asserted them.</param>
    /// <param name="principalId">The local principal.</param>
    [MessageLogging(EventId = 91141, Level = LogLevel.Trace,
        Message = "Subject asserted by '{issuer}' resolved to principal {principalId}")]
    internal static partial IGenericMessage PrincipalResolved(
        ILogger logger, string issuer, Guid principalId);

    /// <summary>Eligibility for issuance was decided.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="principalId">The principal.</param>
    /// <param name="permitted">Whether issuance is permitted.</param>
    /// <param name="reason">Why.</param>
    [MessageLogging(EventId = 91142, Level = LogLevel.Trace,
        Message = "Principal {principalId} issuance permitted={permitted}: {reason}")]
    internal static partial IGenericMessage EligibilityDecided(
        ILogger logger, Guid principalId, bool permitted, string reason);

    /// <summary>A subject with no binding was provisioned a new local principal.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority that asserted them.</param>
    /// <param name="provisionerName">The configured provisioner that created the principal.</param>
    /// <param name="principalId">The newly created local principal.</param>
    [MessageLogging(EventId = 91144, Level = LogLevel.Information,
        Message = "Subject asserted by '{issuer}' had no binding; provisioner '{provisionerName}' created principal {principalId}")]
    internal static partial IGenericMessage PrincipalProvisioned(
        ILogger logger, string issuer, string provisionerName, Guid principalId);

    /// <summary>A provisioner name resolved from a binding row does not resolve to a live provisioner.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority that asserted the subject.</param>
    /// <param name="provisionerName">The provisioner name the binding named.</param>
    [MessageLogging(EventId = 91145, Level = LogLevel.Error,
        Message = "Subject asserted by '{issuer}' is bound to provisioner '{provisionerName}', which did not resolve to a registered provisioner")]
    internal static partial IGenericMessage ProvisionerNotResolved(
        ILogger logger, string issuer, string provisionerName);

    /// <summary>Logs a step asked to run before its Initialize captured what it needs.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stepName">The step the flow named.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 91143, Level = LogLevel.Error,
        Message = "Step '{stepName}' ran before its dependencies were captured, so it cannot do its work. The option's Initialize phase did not run, which means the host was not fully initialized before a flow reached this step")]
    internal static partial IGenericMessage NotInitialized(ILogger logger, string stepName);
}
