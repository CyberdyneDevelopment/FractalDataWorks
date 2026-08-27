using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for the flow runner.
/// </summary>
/// <remarks>
/// EventId range: 91100–91110. Nothing here ever carries a resume token or a credential — an
/// execution is identified by its own id, which is safe to correlate on.
/// </remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class RunnerLog
{
    /// <summary>No flow was supplied.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91100, Level = LogLevel.Error,
        Message = "A flow must be supplied to run one")]
    internal static partial IGenericMessage FlowMissing(ILogger logger);

    /// <summary>A resumed execution belongs to a different flow than the one presented.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The execution.</param>
    /// <param name="recorded">The flow it was suspended under.</param>
    /// <param name="presented">The flow it was resumed against.</param>
    [MessageLogging(EventId = 91101, Level = LogLevel.Error,
        Message = "Execution {executionId} was suspended under flow '{recorded}' and cannot resume as '{presented}'")]
    internal static partial IGenericMessage ExecutionFlowMismatch(
        ILogger logger, Guid executionId, string recorded, string presented);

    /// <summary>A step required something no earlier step contributed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="stepName">The step that could not run.</param>
    /// <param name="missing">What it needed and did not have.</param>
    [MessageLogging(EventId = 91102, Level = LogLevel.Error,
        Message = "Flow '{flowName}' step '{stepName}' requires {missing}, which no earlier step contributed")]
    internal static partial IGenericMessage RequirementMissing(
        ILogger logger, string flowName, string stepName, string missing);

    /// <summary>A step contributed an element it never declared.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stepName">The step.</param>
    /// <param name="element">What it tried to contribute.</param>
    [MessageLogging(EventId = 91103, Level = LogLevel.Error,
        Message = "Step '{stepName}' contributed {element}, which it does not declare — discarded")]
    internal static partial IGenericMessage UndeclaredContribution(
        ILogger logger, string stepName, string element);

    /// <summary>A step declined to act, which is not a failure.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stepName">The step.</param>
    /// <param name="reason">Why it does not apply.</param>
    [MessageLogging(EventId = 91104, Level = LogLevel.Debug,
        Message = "Step '{stepName}' does not apply: {reason}")]
    internal static partial IGenericMessage StepNotApplicable(
        ILogger logger, string stepName, string reason);

    /// <summary>A step returned an outcome this runner does not know.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stepName">The step.</param>
    [MessageLogging(EventId = 91105, Level = LogLevel.Error,
        Message = "Step '{stepName}' returned an outcome this runner cannot act on")]
    internal static partial IGenericMessage UnknownOutcome(ILogger logger, string stepName);

    /// <summary>The flow finished without anyone proving who they are.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    [MessageLogging(EventId = 91106, Level = LogLevel.Error,
        Message = "Flow '{flowName}' completed without a subject — no step proved who this is")]
    internal static partial IGenericMessage NoSubject(ILogger logger, string flowName);

    /// <summary>The flow finished without resolving a local principal.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    [MessageLogging(EventId = 91107, Level = LogLevel.Error,
        Message = "Flow '{flowName}' completed without a principal — the subject was never resolved locally")]
    internal static partial IGenericMessage NoPrincipal(ILogger logger, string flowName);

    /// <summary>Issuance was not permitted.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="reason">Why not.</param>
    [MessageLogging(EventId = 91108, Level = LogLevel.Warning,
        Message = "Flow '{flowName}' will not issue: {reason}")]
    internal static partial IGenericMessage NotPermitted(ILogger logger, string flowName, string reason);

    /// <summary>The methods proved do not reach the level this flow demands.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="achieved">The level reached.</param>
    /// <param name="required">The level demanded.</param>
    [MessageLogging(EventId = 91109, Level = LogLevel.Warning,
        Message = "Flow '{flowName}' reached assurance '{achieved}' but requires '{required}'")]
    internal static partial IGenericMessage InsufficientAssurance(
        ILogger logger, string flowName, string achieved, string required);
}
