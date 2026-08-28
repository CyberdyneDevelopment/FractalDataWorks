#pragma warning disable CS1591
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Charts.Blazor.Logging;

/// <summary>
/// MessageLogging for <c>ChartHost</c> operations.
/// EventId range: 4720–4739 (UI charts host layer).
/// </summary>
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("CHARTS")]
public static partial class ChartHostLog
{
    [MessageLogging(
        EventId = 4720,
        Level = LogLevel.Warning,
        Message = "No Blazor component registered for chart renderer '{rendererName}'")]
    public static partial IGenericMessage RendererNotRegistered(ILogger logger, string rendererName);

    [MessageLogging(
        EventId = 4721,
        Level = LogLevel.Trace,
        Message = "Rendering chart '{chartTitle}' (type='{chartType}') with renderer '{rendererName}'")]
    public static partial IGenericMessage RenderingChart(ILogger logger, string chartTitle, string chartType, string rendererName);

    [MessageLogging(
        EventId = 4722,
        Level = LogLevel.Debug,
        Message = "Selected default chart renderer '{rendererName}' from {rendererCount} registered renderer(s)")]
    public static partial IGenericMessage DefaultRendererChosen(ILogger logger, string rendererName, int rendererCount);

    [MessageLogging(
        EventId = 4723,
        Level = LogLevel.Information,
        Message = "Chart renderer switched to '{rendererName}'")]
    public static partial IGenericMessage RendererChanged(ILogger logger, string rendererName);

    [MessageLogging(
        EventId = 4724,
        Level = LogLevel.Trace,
        Message = "Resolving Blazor component for chart renderer '{rendererName}'")]
    public static partial IGenericMessage ResolvingRendererComponent(ILogger logger, string rendererName);

    [MessageLogging(
        EventId = 4725,
        Level = LogLevel.Error,
        Message = "No chart renderers are registered (ChartRendererTypes is empty) — the chart host cannot render")]
    public static partial IGenericMessage NoRenderersRegistered(ILogger logger);

    [MessageLogging(
        EventId = 4726,
        Level = LogLevel.Information,
        Message = "Chart type changed to '{chartTypeName}' for renderer '{rendererName}'")]
    public static partial IGenericMessage ChartTypeChanged(ILogger logger, string chartTypeName, string rendererName);
}
