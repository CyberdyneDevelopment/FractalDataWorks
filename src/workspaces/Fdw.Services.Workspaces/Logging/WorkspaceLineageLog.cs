using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Logging;

/// <summary>
/// MessageLogging for the lineage views: the neighbourhood of a node, the search for one and the path between two (FDW-1091).
/// </summary>
/// <remarks>Declared beside the map log, so the endpoint package logs through it.</remarks>
[MessageLoggingTypeCode("WORKSPACES")]
public static partial class WorkspaceLineageLog
{
    /// <summary>Logs the neighbourhood a request walked.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="nodeId">The node the walk was centred on.</param>
    /// <param name="direction">The direction walked: Up, Down or Both.</param>
    /// <param name="depth">How many steps the request asked for.</param>
    /// <param name="nodeCount">How many nodes the answer holds.</param>
    /// <param name="edgeCount">How many edges the answer holds.</param>
    /// <param name="truncated">Whether the node cap cut the answer short.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    // Why Debug: one outcome per request, which an operator reads only when tracing a slow or oversized answer.
    [MessageLogging(EventId = 11620, Level = LogLevel.Debug,
        Message = "Lineage neighbourhood of '{nodeId}' ({direction}, depth {depth}): {nodeCount} nodes, {edgeCount} edges, truncated {truncated}")]
    public static partial IGenericMessage NeighbourhoodWalked(
        ILogger logger, string nodeId, string direction, int depth, int nodeCount, int edgeCount, bool truncated);

    /// <summary>Logs how many matches a lineage search found.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="text">The text searched for.</param>
    /// <param name="count">How many matches came back.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    // Why Debug: one outcome per request, as for the neighbourhood.
    [MessageLogging(EventId = 11621, Level = LogLevel.Debug, Message = "Lineage search for '{text}' found {count} matches")]
    public static partial IGenericMessage SearchAnswered(ILogger logger, string text, int count);

    /// <summary>Logs whether a path between two nodes was found.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="source">The node the path starts from.</param>
    /// <param name="target">The node the path should reach.</param>
    /// <param name="maxDepth">The most steps the search was held to.</param>
    /// <param name="outcome">Whether a path was found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    // Why Debug: one outcome per request, as for the neighbourhood.
    [MessageLogging(EventId = 11622, Level = LogLevel.Debug,
        Message = "Lineage path from '{source}' to '{target}' within {maxDepth} steps: {outcome}")]
    public static partial IGenericMessage PathSearched(ILogger logger, string source, string target, int maxDepth, string outcome);
}
