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
    // Why Information: this runs once at startup, and its absence is how an operator notices that
    // no flow loaded at all.
    [MessageLogging(EventId = 91230, Level = LogLevel.Information,
        Message = "Loaded and validated {flowCount} authentication flow(s)")]
    internal static partial IGenericMessage FlowsLoaded(
        ILogger<AuthenticationFlowProvider> logger, int flowCount);

    /// <summary>A caller selected a flow that is not configured.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">What they asked for.</param>
    /// <param name="known">What is configured.</param>
    // Why the known flows are listed: the usual cause is a caller sending a name this host does not
    // serve, and flows are per-host, so "works on the other one" is expected rather than puzzling.
    [MessageLogging(EventId = 91231, Level = LogLevel.Warning,
        Message = "No flow named '{flowName}' is configured here. Configured: {known}")]
    internal static partial IGenericMessage NoSuchFlow(
        ILogger<AuthenticationFlowProvider> logger, string flowName, string known);

    /// <summary>A flow row has no steps.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    // Why Error at load: a flow with no steps proves nothing and would fail at the terminal check,
    // several layers from the row that is actually wrong.
    [MessageLogging(EventId = 91232, Level = LogLevel.Error,
        Message = "Flow '{flowName}' has no steps")]
    internal static partial IGenericMessage FlowHasNoSteps(
        ILogger<AuthenticationFlowProvider> logger, string flowName);

    /// <summary>A configuration table could not be read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="table">Which one.</param>
    // Why Error: no flow can load, so no login works at all until this is fixed.
    [MessageLogging(EventId = 91234, Level = LogLevel.Error,
        Message = "Could not read {table}; no authentication flow can load")]
    internal static partial IGenericMessage RowsUnreadable(
        ILogger<AuthenticationFlowProvider> logger, string table);

    /// <summary>No flow name was supplied.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91233, Level = LogLevel.Error,
        Message = "A flow name must be supplied")]
    internal static partial IGenericMessage NameMissing(ILogger<AuthenticationFlowProvider> logger);
}
