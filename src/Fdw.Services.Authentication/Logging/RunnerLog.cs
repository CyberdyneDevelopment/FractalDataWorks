using System;
using Fdw.MessageLogging;
using Fdw.Services.Authentication.Flow;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for the flow runner.
/// </summary>
/// <remarks>
/// EventId range: 91100–91125. Nothing here ever carries a resume token or a credential — an
/// execution is identified by its own id, which is safe to correlate on, and a flow by its name.
/// <para>
/// Levels are argued, not assumed. Error is a defect — a flow that cannot work as configured, or a
/// step breaking its own contract. Warning is an expected refusal the system handled correctly: a
/// denial, an insufficient assurance level, a resume that did not match. Information marks the two
/// events an operator would want without turning tracing on. Everything on the ordinary path is
/// Trace, because a login that works should cost nothing to log.
/// </para>
/// </remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class RunnerLog
{
    /// <summary>No flow was supplied.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91100, Level = LogLevel.Error,
        Message = "A flow must be supplied to run one")]
    internal static partial IGenericMessage FlowMissing(ILogger<AuthenticationRunner> logger);

    /// <summary>A resumed execution names a flow that no longer resolves.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The execution.</param>
    /// <param name="flowName">The flow it was suspended under.</param>
    [MessageLogging(EventId = 91101, Level = LogLevel.Warning,
        Message = "Execution {executionId} was suspended under flow '{flowName}', which no longer resolves")]
    internal static partial IGenericMessage ResumedFlowNotFound(
        ILogger<AuthenticationRunner> logger, Guid executionId, string flowName);

    /// <summary>A flow names a step no registered option answers to.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stepName">The step name the flow declared.</param>
    [MessageLogging(EventId = 91119, Level = LogLevel.Error,
        Message = "Step '{stepName}' is not registered — its package is not referenced, or the collection changed under a cached flow")]
    internal static partial IGenericMessage StepNotAvailable(ILogger<AuthenticationRunner> logger, string stepName);

    /// <summary>A step required something no earlier step contributed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="stepName">The step that could not run.</param>
    /// <param name="missing">What it needed and did not have.</param>
    [MessageLogging(EventId = 91102, Level = LogLevel.Error,
        Message = "Flow '{flowName}' step '{stepName}' requires {missing}, which no earlier step contributed")]
    internal static partial IGenericMessage RequirementMissing(
        ILogger<AuthenticationRunner> logger, string flowName, string stepName, string missing);

    /// <summary>A step contributed an element it never declared.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stepName">The step.</param>
    /// <param name="element">What it tried to contribute.</param>
    [MessageLogging(EventId = 91103, Level = LogLevel.Error,
        Message = "Step '{stepName}' contributed {element}, which it does not declare — discarded")]
    internal static partial IGenericMessage UndeclaredContribution(
        ILogger<AuthenticationRunner> logger, string stepName, string element);

    /// <summary>A step declined to act, which is not a failure.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stepName">The step.</param>
    /// <param name="reason">Why it does not apply.</param>
    [MessageLogging(EventId = 91104, Level = LogLevel.Debug,
        Message = "Step '{stepName}' does not apply: {reason}")]
    internal static partial IGenericMessage StepNotApplicable(
        ILogger<AuthenticationRunner> logger, string stepName, string reason);

    /// <summary>A step returned an outcome this runner does not know.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stepName">The step.</param>
    [MessageLogging(EventId = 91105, Level = LogLevel.Error,
        Message = "Step '{stepName}' returned an outcome this runner cannot act on")]
    internal static partial IGenericMessage UnknownOutcome(ILogger<AuthenticationRunner> logger, string stepName);

    /// <summary>The flow finished without anyone proving who they are.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    [MessageLogging(EventId = 91106, Level = LogLevel.Error,
        Message = "Flow '{flowName}' completed without a subject — no step proved who this is")]
    internal static partial IGenericMessage NoSubject(ILogger<AuthenticationRunner> logger, string flowName);

    /// <summary>The flow finished without resolving a local principal.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    [MessageLogging(EventId = 91107, Level = LogLevel.Error,
        Message = "Flow '{flowName}' completed without a principal — the subject was never resolved locally")]
    internal static partial IGenericMessage NoPrincipal(ILogger<AuthenticationRunner> logger, string flowName);

    /// <summary>Issuance was not permitted.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="reason">Why not.</param>
    [MessageLogging(EventId = 91108, Level = LogLevel.Warning,
        Message = "Flow '{flowName}' will not issue: {reason}")]
    internal static partial IGenericMessage NotPermitted(ILogger<AuthenticationRunner> logger, string flowName, string reason);

    /// <summary>A flow began.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="stepCount">How many steps it has.</param>
    [MessageLogging(EventId = 91110, Level = LogLevel.Trace,
        Message = "Flow '{flowName}' starting with {stepCount} step(s)")]
    internal static partial IGenericMessage FlowStarting(ILogger<AuthenticationRunner> logger, string flowName, int stepCount);

    /// <summary>A flow resumed from where it suspended.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="executionId">The execution being resumed.</param>
    /// <param name="stepIndex">The step it resumes at.</param>
    [MessageLogging(EventId = 91111, Level = LogLevel.Trace,
        Message = "Flow '{flowName}' resuming execution {executionId} at step {stepIndex}")]
    internal static partial IGenericMessage FlowResuming(
        ILogger<AuthenticationRunner> logger, string flowName, Guid executionId, int stepIndex);

    /// <summary>A step is about to run.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="stepName">The step.</param>
    /// <param name="stepIndex">Its position in the flow.</param>
    [MessageLogging(EventId = 91112, Level = LogLevel.Trace,
        Message = "Flow '{flowName}' step {stepIndex} '{stepName}' executing")]
    internal static partial IGenericMessage StepExecuting(
        ILogger<AuthenticationRunner> logger, string flowName, string stepName, int stepIndex);

    /// <summary>A step produced something the runner kept.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stepName">The step.</param>
    /// <param name="elements">What it contributed.</param>
    [MessageLogging(EventId = 91113, Level = LogLevel.Trace,
        Message = "Step '{stepName}' contributed {elements}")]
    internal static partial IGenericMessage StepContributed(
        ILogger<AuthenticationRunner> logger, string stepName, string elements);

    /// <summary>A step proved an authentication method, which the runner recorded.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stepName">The step.</param>
    /// <param name="method">The RFC 8176 method value recorded.</param>
    [MessageLogging(EventId = 91114, Level = LogLevel.Trace,
        Message = "Step '{stepName}' proved method '{method}'")]
    internal static partial IGenericMessage MethodRecorded(ILogger<AuthenticationRunner> logger, string stepName, string method);

    /// <summary>The achieved methods were evaluated to an assurance level.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="methods">The methods proved.</param>
    /// <param name="acr">The level they amount to.</param>
    [MessageLogging(EventId = 91115, Level = LogLevel.Trace,
        Message = "Methods [{methods}] evaluated to assurance '{acr}'")]
    internal static partial IGenericMessage AssuranceEvaluated(ILogger<AuthenticationRunner> logger, string methods, string acr);

    /// <summary>Every terminal condition passed and a token is being minted.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="audience">The audience the token is minted for.</param>
    [MessageLogging(EventId = 91116, Level = LogLevel.Trace,
        Message = "Flow '{flowName}' passed its terminal check; issuing for audience '{audience}'")]
    internal static partial IGenericMessage TerminalPassed(ILogger<AuthenticationRunner> logger, string flowName, string audience);

    /// <summary>A flow suspended, waiting for its caller.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="stepName">The step that suspended it.</param>
    [MessageLogging(EventId = 91117, Level = LogLevel.Information,
        Message = "Flow '{flowName}' suspended at step '{stepName}', awaiting the caller")]
    internal static partial IGenericMessage FlowSuspended(ILogger<AuthenticationRunner> logger, string flowName, string stepName);

    /// <summary>A flow completed and a token was issued.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="methods">The methods proved.</param>
    /// <param name="acr">The assurance level reached.</param>
    [MessageLogging(EventId = 91118, Level = LogLevel.Information,
        Message = "Flow '{flowName}' issued a token; methods [{methods}], assurance '{acr}'")]
    internal static partial IGenericMessage FlowCompleted(
        ILogger<AuthenticationRunner> logger, string flowName, string methods, string acr);

    /// <summary>The methods proved do not reach the level this flow demands.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="achieved">The level reached.</param>
    /// <param name="required">The level demanded.</param>
    [MessageLogging(EventId = 91109, Level = LogLevel.Warning,
        Message = "Flow '{flowName}' reached assurance '{achieved}' but requires '{required}'")]
    internal static partial IGenericMessage InsufficientAssurance(
        ILogger<AuthenticationRunner> logger, string flowName, string achieved, string required);
}
