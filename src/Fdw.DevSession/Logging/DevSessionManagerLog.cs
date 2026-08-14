using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.DevSession.Logging;

/// <summary>
/// MessageLogging for the development session manager.
/// EventId range: 12030-12049 (trace/info), 92030-92049 (errors).
/// </summary>
[MessageLoggingTypeCode("DEVSESSION")]
public static partial class DevSessionManagerLog
{
    [MessageLogging(
        EventId = 12030,
        Level = LogLevel.Information,
        Message = "Opened session {sessionId} for key {key} on branch {branchName}")]
    public static partial IGenericMessage SessionOpened(
        ILogger logger,
        Guid sessionId,
        string key,
        string branchName);

    [MessageLogging(
        EventId = 12031,
        Level = LogLevel.Trace,
        Message = "Reusing open session {sessionId} for key {key}")]
    public static partial IGenericMessage SessionReused(
        ILogger logger,
        Guid sessionId,
        string key);

    [MessageLogging(
        EventId = 12032,
        Level = LogLevel.Information,
        Message = "Opened nested session {sessionId} under parent {parentSessionId} for key {key}")]
    public static partial IGenericMessage NestedSessionOpened(
        ILogger logger,
        Guid sessionId,
        Guid parentSessionId,
        string key);

    [MessageLogging(
        EventId = 12033,
        Level = LogLevel.Information,
        Message = "Session {sessionId} transitioned from {fromState} to {toState}")]
    public static partial IGenericMessage SessionTransitioned(
        ILogger logger,
        Guid sessionId,
        string fromState,
        string toState);

    [MessageLogging(
        EventId = 92030,
        Level = LogLevel.Error,
        Message = "No session found with id {sessionId}")]
    public static partial IGenericMessage SessionNotFoundById(
        ILogger logger,
        Guid sessionId);

    [MessageLogging(
        EventId = 92031,
        Level = LogLevel.Error,
        Message = "No session found for key {key}")]
    public static partial IGenericMessage SessionNotFoundByKey(
        ILogger logger,
        string key);

    [MessageLogging(
        EventId = 92032,
        Level = LogLevel.Error,
        Message = "Unknown isolation level '{isolationLevelName}' — it is not a registered IsolationLevels option")]
    public static partial IGenericMessage UnknownIsolationLevel(
        ILogger logger,
        string isolationLevelName);

    [MessageLogging(
        EventId = 92033,
        Level = LogLevel.Error,
        Message = "Session {sessionId} is in terminal state {state} and cannot be {attemptedOperation}")]
    public static partial IGenericMessage SessionIsTerminal(
        ILogger logger,
        Guid sessionId,
        string state,
        string attemptedOperation);

    [MessageLogging(
        EventId = 92034,
        Level = LogLevel.Error,
        Message = "Session {sessionId} is {state}, so it cannot be {attemptedOperation}")]
    public static partial IGenericMessage InvalidTransition(
        ILogger logger,
        Guid sessionId,
        string state,
        string attemptedOperation);

    [MessageLogging(
        EventId = 92035,
        Level = LogLevel.Error,
        Message = "Isolation level '{isolationLevelName}' failed to materialize the copy for key {key}")]
    public static partial IGenericMessage MaterializeFailed(
        ILogger logger,
        string isolationLevelName,
        string key);

    [MessageLogging(
        EventId = 92036,
        Level = LogLevel.Error,
        Message = "Required session state '{stateName}' is not registered in SessionStates")]
    public static partial IGenericMessage StateNotRegistered(
        ILogger logger,
        string stateName);
}
