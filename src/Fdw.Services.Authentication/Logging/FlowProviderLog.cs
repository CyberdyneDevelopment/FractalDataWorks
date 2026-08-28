using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Services.Authentication.Flow;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for loading flows from configuration.
/// </summary>
/// <remarks>EventId range: 91230–91234.</remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class FlowProviderLog
{
    /// <summary>Every configured flow loaded and validated.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowCount">How many.</param>
    [MessageLogging(EventId = 91230, Level = LogLevel.Information,
        Message = "Loaded and validated {flowCount} authentication flow(s)")]
    internal static partial IGenericMessage FlowsLoaded(
        ILogger<AuthenticationFlowProvider> logger, int flowCount);

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

    /// <summary>A caller selected a flow that is not configured.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">What they asked for.</param>
    /// <param name="known">What is configured.</param>
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
}
