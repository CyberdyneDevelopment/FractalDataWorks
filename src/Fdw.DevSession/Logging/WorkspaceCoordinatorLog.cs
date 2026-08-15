using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.DevSession.Logging;

/// <summary>
/// MessageLogging for the workspace coordinator (strand concurrency within one session).
/// EventId range: 12050-12069 (trace/info), 92050-92069 (errors).
/// </summary>
[MessageLoggingTypeCode("DEVSESSION")]
public static partial class WorkspaceCoordinatorLog
{
    [MessageLogging(
        EventId = 12050,
        Level = LogLevel.Information,
        Message = "Fenced strand {strandId} in session {sessionId} over {pathCount} path(s)")]
    public static partial IGenericMessage StrandFenced(
        ILogger logger,
        string strandId,
        Guid sessionId,
        int pathCount);

    [MessageLogging(
        EventId = 12051,
        Level = LogLevel.Information,
        Message = "Routed strand {strandId} to handler {handlerName}")]
    public static partial IGenericMessage StrandRouted(
        ILogger logger,
        string strandId,
        string handlerName);

    [MessageLogging(
        EventId = 12052,
        Level = LogLevel.Information,
        Message = "Reconciled strand {strandId} in session {sessionId}")]
    public static partial IGenericMessage StrandReconciled(
        ILogger logger,
        string strandId,
        Guid sessionId);

    [MessageLogging(
        EventId = 92050,
        Level = LogLevel.Error,
        Message = "Strand {strandId} cannot be fenced: its paths overlap those already claimed by strand {conflictingStrandId}")]
    public static partial IGenericMessage ScopeOverlap(
        ILogger logger,
        string strandId,
        string conflictingStrandId);

    [MessageLogging(
        EventId = 92051,
        Level = LogLevel.Error,
        Message = "Strand {strandId} is already fenced in session {sessionId}")]
    public static partial IGenericMessage StrandAlreadyFenced(
        ILogger logger,
        string strandId,
        Guid sessionId);

    [MessageLogging(
        EventId = 92052,
        Level = LogLevel.Error,
        Message = "No strand {strandId} in session {sessionId}")]
    public static partial IGenericMessage StrandNotFound(
        ILogger logger,
        string strandId,
        Guid sessionId);

    [MessageLogging(
        EventId = 92053,
        Level = LogLevel.Error,
        Message = "No registered StrandHandlers option can handle strand {strandId}")]
    public static partial IGenericMessage NoHandlerForStrand(
        ILogger logger,
        string strandId);

    [MessageLogging(
        EventId = 92054,
        Level = LogLevel.Error,
        Message = "Strand {strandId} is in terminal state {state} and cannot be reconciled")]
    public static partial IGenericMessage StrandIsTerminal(
        ILogger logger,
        string strandId,
        string state);

    [MessageLogging(
        EventId = 92055,
        Level = LogLevel.Error,
        Message = "A scope request must claim at least one path (strand {strandId})")]
    public static partial IGenericMessage EmptyScope(
        ILogger logger,
        string strandId);

    [MessageLogging(
        EventId = 92056,
        Level = LogLevel.Error,
        Message = "Required strand state '{stateName}' is not registered in StrandStates")]
    public static partial IGenericMessage StrandStateNotRegistered(
        ILogger logger,
        string stateName);
}
