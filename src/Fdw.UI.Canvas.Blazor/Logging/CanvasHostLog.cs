#pragma warning disable CS1591
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Canvas.Blazor.Logging;

/// <summary>
/// MessageLogging for <c>CanvasHost</c> operations.
/// EventId range: 4700–4719 (UI canvas host layer).
/// </summary>
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("CANVAS")]
public static partial class CanvasHostLog
{
    [MessageLogging(
        EventId = 4700,
        Level = LogLevel.Warning,
        Message = "No Blazor component registered for canvas renderer '{rendererName}'")]
    public static partial IGenericMessage RendererNotRegistered(ILogger logger, string rendererName);

    [MessageLogging(
        EventId = 4701,
        Level = LogLevel.Trace,
        Message = "Rendering canvas '{canvasTitle}' ({nodeCount} node(s), {edgeCount} edge(s)) with renderer '{rendererName}'")]
    public static partial IGenericMessage RenderingCanvas(ILogger logger, string canvasTitle, int nodeCount, int edgeCount, string rendererName);

    [MessageLogging(
        EventId = 4702,
        Level = LogLevel.Debug,
        Message = "Selected default canvas renderer '{rendererName}' (editMode={editMode}) from {rendererCount} registered renderer(s)")]
    public static partial IGenericMessage DefaultRendererChosen(ILogger logger, string rendererName, bool editMode, int rendererCount);

    [MessageLogging(
        EventId = 4703,
        Level = LogLevel.Information,
        Message = "Canvas renderer switched to '{rendererName}'")]
    public static partial IGenericMessage RendererChanged(ILogger logger, string rendererName);

    [MessageLogging(
        EventId = 4704,
        Level = LogLevel.Trace,
        Message = "Resolving Blazor component for canvas renderer '{rendererName}'")]
    public static partial IGenericMessage ResolvingRendererComponent(ILogger logger, string rendererName);

    [MessageLogging(
        EventId = 4705,
        Level = LogLevel.Error,
        Message = "No canvas renderers are registered (CanvasRendererTypes is empty) — the canvas host cannot render")]
    public static partial IGenericMessage NoRenderersRegistered(ILogger logger);
}
