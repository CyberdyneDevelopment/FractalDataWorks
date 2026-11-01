using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Logging;

/// <summary>
/// MessageLogging for the combined lineage and workspace map (FDW-1029).
/// </summary>
/// <remarks>Declared beside the other workspace logs, so the endpoint package logs through it.</remarks>
[MessageLoggingTypeCode("WORKSPACES")]
public static partial class WorkspaceMapLog
{
    /// <summary>Logs that a map is about to be streamed.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="scope">The scope the map covers: everything, an organization or a workspace.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11610, Level = LogLevel.Information, Message = "Streaming the map for scope '{scope}'")]
    public static partial IGenericMessage MapStarted(ILogger logger, string scope);

    /// <summary>Logs how a streamed map ended.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="nodeCount">How many nodes were sent.</param>
    /// <param name="edgeCount">How many edges were sent.</param>
    /// <param name="unresolved">How many references named an object that was not there.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11611, Level = LogLevel.Information,
        Message = "Streamed the map: {nodeCount} nodes, {edgeCount} edges, {unresolved} unresolved references")]
    public static partial IGenericMessage MapStreamed(ILogger logger, int nodeCount, int edgeCount, int unresolved);

    /// <summary>Logs that one node is about to be exploded.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="nodeId">The node's id.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11612, Level = LogLevel.Information, Message = "Exploding map node '{nodeId}'")]
    public static partial IGenericMessage NodeExploding(ILogger logger, string nodeId);

    /// <summary>Logs how an explode ended.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="nodeId">The node's id.</param>
    /// <param name="nodeCount">How many child nodes were sent.</param>
    /// <param name="edgeCount">How many edges were sent.</param>
    /// <param name="unresolved">How many references named an object that was not there.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11613, Level = LogLevel.Information,
        Message = "Exploded map node '{nodeId}': {nodeCount} nodes, {edgeCount} edges, {unresolved} unresolved references")]
    public static partial IGenericMessage NodeExploded(ILogger logger, string nodeId, int nodeCount, int edgeCount, int unresolved);

    /// <summary>Logs that a node's kind has nothing under it.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="nodeId">The node's id.</param>
    /// <param name="kind">The node's kind.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11614, Level = LogLevel.Information, Message = "Map node '{nodeId}' is a {kind}, which has no children to explode")]
    public static partial IGenericMessage NodeHasNoChildren(ILogger logger, string nodeId, string kind);

    /// <summary>Logs a reference the map could not resolve: something named an object that is not there.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="kind">The kind of the object that was named.</param>
    /// <param name="reference">The name or id it was named by.</param>
    /// <param name="namedBy">What named it.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31050, Level = LogLevel.Warning,
        Message = "Map reference unresolved: {kind} '{reference}' named by {namedBy} was not found, so it is left off the map")]
    public static partial IGenericMessage ReferenceUnresolved(ILogger logger, string kind, string reference, string namedBy);
}
