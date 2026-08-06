#pragma warning disable CS1591
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Canvas.Blazor.Logging;

/// <summary>
/// MessageLogging for the Blazor Diagrams canvas renderer (<c>BlazorDiagramsRenderer</c>).
/// EventId range: 4750–4759 (UI canvas Blazor Diagrams renderer layer).
/// </summary>
// Why: default TypeCode ("FDW") collided with Fdw.UI.Charts.Blazor's RadzenChartsRendererLog,
// which independently reused EventIds 4750-4752 — both generated "FDW-4750"/"FDW-4751"/"FDW-4752"
// for unrelated messages. A distinct per-project TypeCode makes the generated Code unique even
// though the numeric EventId ranges still overlap across the two sibling UI projects.
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("CANVAS")]
public static partial class BlazorDiagramsRendererLog
{
    [MessageLogging(
        EventId = 4750,
        Level = LogLevel.Trace,
        Message = "Building BlazorDiagram from model: {nodeCount} node(s), {edgeCount} edge(s)")]
    public static partial IGenericMessage Building(ILogger logger, int nodeCount, int edgeCount);

    [MessageLogging(
        EventId = 4751,
        Level = LogLevel.Information,
        Message = "BlazorDiagram built: {nodeCount} node(s), {edgeCount} edge(s)")]
    public static partial IGenericMessage Built(ILogger logger, int nodeCount, int edgeCount);

    // Why: Warning, not Error — the canvas still renders; only the orphaned edge is silently skipped.
    [MessageLogging(
        EventId = 4752,
        Level = LogLevel.Warning,
        Message = "Edge '{edgeId}' skipped — source node '{sourceNodeId}' or target node '{targetNodeId}' not found in model")]
    public static partial IGenericMessage EdgeNodeNotFound(ILogger logger, string edgeId, string sourceNodeId, string targetNodeId);

    [MessageLogging(
        EventId = 4753,
        Level = LogLevel.Trace,
        Message = "BlazorDiagram rebuilt after model reference change")]
    public static partial IGenericMessage DiagramRebuilt(ILogger logger);
}
