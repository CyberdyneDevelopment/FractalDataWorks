using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Agents.Components.Logging;

/// <summary>
/// MessageLogging methods for AgentActionProvider operations.
/// EventId range: 4140-4149
/// </summary>
[MessageLoggingTypeCode("COMPONENTS")]
public static partial class AgentActionProviderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Load Actions (4140-4141)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading the agent action list fails.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Warning,
        Message = "AgentActionProvider: Failed to load agent actions")]
    public static partial IGenericMessage LoadActionsFailed(ILogger logger);

    /// <summary>Logs when loading the agent action list throws an exception.</summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Warning,
        Message = "AgentActionProvider: Exception loading agent actions")]
    public static partial IGenericMessage LoadActionsException(ILogger logger, Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Get Action (4142-4143)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when getting a single agent action fails.</summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Warning,
        Message = "AgentActionProvider: Failed to get agent action '{actionId}'")]
    public static partial IGenericMessage GetActionFailed(ILogger logger, int actionId);

    /// <summary>Logs when getting a single agent action throws an exception.</summary>
    [MessageLogging(EventId = 71003, Level = LogLevel.Warning,
        Message = "AgentActionProvider: Exception getting agent action")]
    public static partial IGenericMessage GetActionException(ILogger logger, Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Approve Action (4144-4145)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when approving an agent action fails.</summary>
    [MessageLogging(EventId = 71004, Level = LogLevel.Warning,
        Message = "AgentActionProvider: Failed to approve agent action '{actionId}'")]
    public static partial IGenericMessage ApproveActionFailed(ILogger logger, int actionId);

    /// <summary>Logs when approving an agent action throws an exception.</summary>
    [MessageLogging(EventId = 71005, Level = LogLevel.Warning,
        Message = "AgentActionProvider: Exception approving agent action")]
    public static partial IGenericMessage ApproveActionException(ILogger logger, Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Deny Action (4146-4147)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when denying an agent action fails.</summary>
    [MessageLogging(EventId = 71006, Level = LogLevel.Warning,
        Message = "AgentActionProvider: Failed to deny agent action '{actionId}'")]
    public static partial IGenericMessage DenyActionFailed(ILogger logger, int actionId);

    /// <summary>Logs when denying an agent action throws an exception.</summary>
    [MessageLogging(EventId = 71007, Level = LogLevel.Warning,
        Message = "AgentActionProvider: Exception denying agent action")]
    public static partial IGenericMessage DenyActionException(ILogger logger, Exception exception);
}
