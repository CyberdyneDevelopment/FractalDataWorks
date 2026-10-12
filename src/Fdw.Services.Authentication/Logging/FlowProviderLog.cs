using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Services.Authentication.Flow;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for loading flows from configuration.
/// </summary>
/// <remarks>EventId range: 91230–91239, continued at 91121–91123.</remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class FlowProviderLog
{
    /// <summary>Every configured flow row was read and judged; some may have failed on their own.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="validCount">How many are usable.</param>
    /// <param name="totalCount">How many rows exist, usable or not.</param>
    [MessageLogging(EventId = 91230, Level = LogLevel.Information,
        Message = "Loaded {validCount}/{totalCount} authentication flow(s) as valid")]
    internal static partial IGenericMessage FlowsLoaded(
        ILogger<AuthenticationFlowProvider> logger, int validCount, int totalCount);

    /// <summary>A configured flow was assembled from its rows.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="stepCount">How many steps it names.</param>
    [MessageLogging(EventId = 91235, Level = LogLevel.Trace,
        Message = "Assembled flow '{flowName}' from {stepCount} step row(s)")]
    internal static partial IGenericMessage FlowAssembled(
        ILogger<AuthenticationFlowProvider> logger, string flowName, int stepCount);

    /// <summary>A flow was served from cache.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    [MessageLogging(EventId = 91236, Level = LogLevel.Trace,
        Message = "Served flow '{flowName}' from cache")]
    internal static partial IGenericMessage FlowServed(
        ILogger<AuthenticationFlowProvider> logger, string flowName);

    /// <summary>A caller selected a flow that is not configured at all.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">What they asked for.</param>
    /// <param name="known">The flows that are configured and valid.</param>
    [MessageLogging(EventId = 91231, Level = LogLevel.Warning,
        Message = "No flow named '{flowName}' is configured here. Configured: {known}")]
    internal static partial IGenericMessage NoSuchFlow(
        ILogger<AuthenticationFlowProvider> logger, string flowName, string known);

    /// <summary>A flow row has no steps.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    [MessageLogging(EventId = 91232, Level = LogLevel.Error,
        Message = "Flow '{flowName}' has no steps")]
    internal static partial IGenericMessage FlowHasNoSteps(
        ILogger<AuthenticationFlowProvider> logger, string flowName);

    /// <summary>A configuration table could not be read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="table">Which one.</param>
    // Critical, not Error: No flow can load, so the host cannot authenticate anyone at all. The service is running and cannot do the
    // one thing it exists for.
    [MessageLogging(EventId = 91234, Level = LogLevel.Critical,
        Message = "Could not read {table}; no authentication flow can load")]
    internal static partial IGenericMessage RowsUnreadable(
        ILogger<AuthenticationFlowProvider> logger, string table);

    /// <summary>No flow name was supplied.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91233, Level = LogLevel.Error,
        Message = "A flow name must be supplied")]
    internal static partial IGenericMessage NameMissing(ILogger<AuthenticationFlowProvider> logger);

    /// <summary>Logs a flow naming a step no option answers to.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="stepName">The step it named.</param>
    /// <param name="known">The steps that are available.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 91237, Level = LogLevel.Error,
        Message = "Flow '{flowName}' names step '{stepName}' and no option answers to it. Available: {known}. An option joins the collection by being referenced, so this is usually a missing package rather than a wrong name")]
    internal static partial IGenericMessage StepNotAvailable(
        ILogger logger, string flowName, string stepName, string known);

    /// <summary>Logs a flow whose steps are ordered so one runs before what it needs exists.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="stepName">The step that would run too early.</param>
    /// <param name="missing">What it needs that nothing before it establishes.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 91238, Level = LogLevel.Error,
        Message = "Flow '{flowName}' runs step '{stepName}' before {missing} exists. Each step declares what it requires and what it contributes, and the order has to satisfy that — caught here, when configuration loads, rather than at someone's login")]
    internal static partial IGenericMessage OrderInvalid(
        ILogger logger, string flowName, string stepName, string missing);

    /// <summary>A caller selected a flow that is configured but failed its own validation.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">What they asked for.</param>
    /// <param name="reason">Why this flow specifically failed — the message its own validation logged.</param>
    [MessageLogging(EventId = 91239, Level = LogLevel.Warning,
        Message = "Flow '{flowName}' is configured but invalid: {reason}")]
    internal static partial IGenericMessage FlowKnownInvalid(
        ILogger<AuthenticationFlowProvider> logger, string flowName, string reason);

    /// <summary>A caller selected a flow an operator has switched off.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">What they asked for.</param>
    // Warning, not Error: nothing is broken. The host refused exactly as configured, and an operator
    // reading the log wants to see that somebody still tries a switched-off flow.
    [MessageLogging(EventId = 91121, Level = LogLevel.Warning,
        Message = "Flow '{flowName}' is switched off (IsEnabled = 0) and was refused")]
    internal static partial IGenericMessage FlowDisabled(
        ILogger<AuthenticationFlowProvider> logger, string flowName);

    /// <summary>A flow row loaded switched off, so it was not validated or offered.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    // Information: an operator switching a flow off wants to see, once at load, that the host took it.
    [MessageLogging(EventId = 91122, Level = LogLevel.Information,
        Message = "Flow '{flowName}' is switched off; it is neither validated nor offered until IsEnabled is set")]
    internal static partial IGenericMessage FlowLoadedDisabled(
        ILogger<AuthenticationFlowProvider> logger, string flowName);

    /// <summary>A configured step refused the settings a flow gives it.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    /// <param name="stepName">The step.</param>
    /// <param name="reason">What the step said is wrong.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 91123, Level = LogLevel.Error,
        Message = "Flow '{flowName}' gives step '{stepName}' settings it cannot run with: {reason}. Fix the step row's Configuration column")]
    internal static partial IGenericMessage StepConfigurationRejected(
        ILogger logger, string flowName, string stepName, string reason);
}
