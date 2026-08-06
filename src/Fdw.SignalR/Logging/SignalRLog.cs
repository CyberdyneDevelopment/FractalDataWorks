using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.SignalR;

/// <summary>
/// MessageLogging class for SignalR broadcast operations.
/// EventId range: 9100-9199
/// </summary>
[MessageLoggingTypeCode("SIGNALR2")]
public static partial class SignalRLog
{
    // ========================================================================
    // Broadcast Operations (EventId 9100-9119)
    // ========================================================================

    /// <summary>
    /// Logs when a broadcaster starts broadcasting to multiple groups.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Debug,
        Message = "Broadcaster {broadcasterName} starting broadcast of {eventName} to {groupCount} groups")]
    public static partial IGenericMessage BroadcastStarting(
        ILogger logger,
        string broadcasterName,
        string eventName,
        int groupCount);

    /// <summary>
    /// Logs when a broadcaster completes broadcasting to multiple groups.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Debug,
        Message = "Broadcaster {broadcasterName} completed broadcast of {eventName} to {groupCount} groups")]
    public static partial IGenericMessage BroadcastCompleted(
        ILogger logger,
        string broadcasterName,
        string eventName,
        int groupCount);

    /// <summary>
    /// Logs when a broadcast operation fails.
    /// </summary>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Warning,
        Message = "Broadcaster {broadcasterName} failed to broadcast {eventName}: {errorMessage}")]
    public static partial IGenericMessage BroadcastFailed(
        ILogger logger,
        string broadcasterName,
        string eventName,
        string errorMessage);

    /// <summary>
    /// Logs when a broadcast operation fails, including the full exception for stack trace capture.
    /// </summary>
    [MessageLogging(
        EventId = 71001,
        Level = LogLevel.Warning,
        Message = "Broadcaster {broadcasterName} failed to broadcast {eventName}")]
    public static partial IGenericMessage BroadcastFailed(
        ILogger logger,
        Exception ex,
        string broadcasterName,
        string eventName);

    /// <summary>
    /// Logs when a broadcaster starts broadcasting to all clients.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Debug,
        Message = "Broadcaster {broadcasterName} starting broadcast of {eventName} to all clients")]
    public static partial IGenericMessage BroadcastToAllStarting(
        ILogger logger,
        string broadcasterName,
        string eventName);

    /// <summary>
    /// Logs when a broadcaster completes broadcasting to all clients.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "Broadcaster {broadcasterName} completed broadcast of {eventName} to all clients")]
    public static partial IGenericMessage BroadcastToAllCompleted(
        ILogger logger,
        string broadcasterName,
        string eventName);

    // ========================================================================
    // Hub Connection Operations (EventId 9120-9139)
    // ========================================================================

    /// <summary>
    /// Logs when a client connects to a SignalR hub.
    /// </summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Information,
        Message = "Client {connectionId} connected to hub {hubName}")]
    public static partial IGenericMessage ClientConnected(
        ILogger logger,
        string connectionId,
        string hubName);

    /// <summary>
    /// Logs when a client disconnects from a SignalR hub.
    /// </summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Information,
        Message = "Client {connectionId} disconnected from hub {hubName}")]
    public static partial IGenericMessage ClientDisconnected(
        ILogger logger,
        string connectionId,
        string hubName);

    /// <summary>
    /// Logs when a client disconnects from a SignalR hub with an error.
    /// </summary>
    [MessageLogging(
        EventId = 71002,
        Level = LogLevel.Warning,
        Message = "Client {connectionId} disconnected from hub {hubName} with error: {errorMessage}")]
    public static partial IGenericMessage ClientDisconnectedWithError(
        ILogger logger,
        string connectionId,
        string hubName,
        string errorMessage);

    // ========================================================================
    // Group Operations (EventId 9140-9159)
    // ========================================================================

    /// <summary>
    /// Logs when a client joins a SignalR group.
    /// </summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Debug,
        Message = "Client {connectionId} joined group {groupName}")]
    public static partial IGenericMessage ClientJoinedGroup(
        ILogger logger,
        string connectionId,
        string groupName);

    /// <summary>
    /// Logs when a client leaves a SignalR group.
    /// </summary>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Debug,
        Message = "Client {connectionId} left group {groupName}")]
    public static partial IGenericMessage ClientLeftGroup(
        ILogger logger,
        string connectionId,
        string groupName);

    // ========================================================================
    // Registration Operations (EventId 9160-9179)
    // ========================================================================

    /// <summary>
    /// Logs when a SignalR broadcaster is registered with dependency injection.
    /// </summary>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Information,
        Message = "Registered broadcaster {broadcasterType} for hub {hubType}")]
    public static partial IGenericMessage BroadcasterRegistered(
        ILogger logger,
        string broadcasterType,
        string hubType);

    // ========================================================================
    // RealTimeHubs Collection: Registration + Mapping (EventId 11009-11014)
    // ========================================================================

    /// <summary>
    /// Logs when the RealTimeHubs collection begins registering hub services.
    /// </summary>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Information,
        Message = "Registering services for {hubCount} real-time hub(s)")]
    public static partial IGenericMessage RealTimeHubsRegistering(
        ILogger logger,
        int hubCount);

    /// <summary>
    /// Logs when a single real-time hub's services are registered.
    /// </summary>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Information,
        Message = "Registered services for real-time hub {hubName} ({hubType}) at {route}")]
    public static partial IGenericMessage RealTimeHubServicesRegistered(
        ILogger logger,
        string hubName,
        string hubType,
        string route);

    /// <summary>
    /// Logs when the RealTimeHubs collection has registered all hub services.
    /// </summary>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Information,
        Message = "Registered services for {hubCount} real-time hub(s)")]
    public static partial IGenericMessage RealTimeHubsRegistered(
        ILogger logger,
        int hubCount);

    /// <summary>
    /// Logs when the RealTimeHubs collection begins mapping hub endpoints.
    /// </summary>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Information,
        Message = "Mapping {hubCount} real-time hub endpoint(s)")]
    public static partial IGenericMessage RealTimeHubsMapping(
        ILogger logger,
        int hubCount);

    /// <summary>
    /// Logs when a single real-time hub endpoint is mapped.
    /// </summary>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Information,
        Message = "Mapped real-time hub {hubName} ({hubType}) at {route}")]
    public static partial IGenericMessage RealTimeHubMapped(
        ILogger logger,
        string hubName,
        string hubType,
        string route);

    /// <summary>
    /// Logs when the RealTimeHubs collection has mapped all hub endpoints.
    /// </summary>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Information,
        Message = "Mapped {hubCount} real-time hub endpoint(s)")]
    public static partial IGenericMessage RealTimeHubsMapped(
        ILogger logger,
        int hubCount);

    // ========================================================================
    // Subscription Guards + Identity (EventId 71003-71005)
    // ========================================================================

    /// <summary>
    /// Logs when a subscription request is rejected because the scope key was empty.
    /// </summary>
    [MessageLogging(
        EventId = 71003,
        Level = LogLevel.Warning,
        Message = "Connection {connectionId} on hub {hubName} sent an empty subscription scope; ignored")]
    public static partial IGenericMessage SubscriptionRejectedEmptyScope(
        ILogger logger,
        string connectionId,
        string hubName);

    /// <summary>
    /// Logs when a subscription request is rejected because the caller is not authorized for the scope.
    /// </summary>
    [MessageLogging(
        EventId = 71004,
        Level = LogLevel.Warning,
        Message = "Connection {connectionId} on hub {hubName} not authorized to subscribe to {scopeKey}; ignored")]
    public static partial IGenericMessage SubscriptionRejectedNotAuthorized(
        ILogger logger,
        string connectionId,
        string hubName,
        string scopeKey);

    /// <summary>
    /// Logs when an authenticated hub connection carries no resolvable identity, so a scoped
    /// auto-join is skipped instead of falling back to a placeholder identity.
    /// </summary>
    [MessageLogging(
        EventId = 71005,
        Level = LogLevel.Warning,
        Message = "Connection {connectionId} on hub {hubName} has no authenticated identity; skipping scoped auto-join")]
    public static partial IGenericMessage HubIdentityMissing(
        ILogger logger,
        string connectionId,
        string hubName);

    /// <summary>
    /// Logs when a hub connection carries no <c>org_id</c> claim, so the org-scoped firehose auto-join
    /// is skipped — the connection joins no global (cross-org) firehose and no placeholder org is used.
    /// </summary>
    [MessageLogging(
        EventId = 71006,
        Level = LogLevel.Warning,
        Message = "Connection {connectionId} on hub {hubName} has no org_id claim; skipping org firehose auto-join")]
    public static partial IGenericMessage HubOrgClaimMissing(
        ILogger logger,
        string connectionId,
        string hubName);
}
