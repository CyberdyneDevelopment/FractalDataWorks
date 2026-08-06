#pragma warning disable CS1591
using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Canvas.Blazor.Logging;

/// <summary>
/// MessageLogging for the Cytoscape.js canvas renderer (<c>CytoscapeRenderer</c>).
/// EventId range: 4760–4769 (UI canvas Cytoscape renderer layer).
/// </summary>
// Why: give every Fdw.UI.Canvas.Blazor Log class its own TypeCode so its generated Code never
// collides with the sibling Fdw.UI.Charts.Blazor project, which shares the same 47xx EventId band.
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("CANVAS")]
public static partial class CytoscapeRendererLog
{
    [MessageLogging(
        EventId = 4760,
        Level = LogLevel.Trace,
        Message = "Importing Cytoscape JS interop module")]
    public static partial IGenericMessage LoadingModule(ILogger logger);

    [MessageLogging(
        EventId = 4761,
        Level = LogLevel.Information,
        Message = "Cytoscape JS interop module loaded")]
    public static partial IGenericMessage ModuleLoaded(ILogger logger);

    [MessageLogging(
        EventId = 4762,
        Level = LogLevel.Trace,
        Message = "Rendering Cytoscape graph: {nodeCount} nodes, {edgeCount} edges, layout '{layoutName}'")]
    public static partial IGenericMessage RenderingGraph(
        ILogger logger,
        int nodeCount,
        int edgeCount,
        string layoutName);

    [MessageLogging(
        EventId = 4763,
        Level = LogLevel.Information,
        Message = "Cytoscape graph rendered: {nodeCount} nodes, {edgeCount} edges")]
    public static partial IGenericMessage GraphRendered(ILogger logger, int nodeCount, int edgeCount);

    // Why: Error level — JS render failure means the canvas is blank; the operator needs to know.
    [MessageLogging(
        EventId = 4764,
        Level = LogLevel.Error,
        Message = "Cytoscape graph render failed: {reason}")]
    public static partial IGenericMessage RenderFailed(ILogger logger, Exception exception, string reason);

    [MessageLogging(
        EventId = 4765,
        Level = LogLevel.Trace,
        Message = "Cytoscape: JS interop interrupted during teardown/navigation (expected)")]
    public static partial IGenericMessage TeardownInterrupted(ILogger logger, Exception exception);
}
