#pragma warning disable CS1591
using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Charts.Blazor.Logging;

/// <summary>
/// MessageLogging for <c>EChartsRenderer</c> operations.
/// EventId range: 4770–4779 (UI charts ECharts renderer layer).
/// </summary>
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("CHARTS")]
public static partial class EChartsRendererLog
{
    [MessageLogging(
        EventId = 4770,
        Level = LogLevel.Trace,
        Message = "EChartsRenderer: beginning render for chart type '{chartTypeName}' with {rowCount} row(s)")]
    public static partial IGenericMessage RenderBegin(ILogger logger, string chartTypeName, int rowCount);

    [MessageLogging(
        EventId = 4771,
        Level = LogLevel.Information,
        Message = "EChartsRenderer: rendered chart type '{chartTypeName}' with {rowCount} row(s)")]
    public static partial IGenericMessage Rendered(ILogger logger, string chartTypeName, int rowCount);

    [MessageLogging(
        EventId = 4772,
        Level = LogLevel.Warning,
        Message = "EChartsRenderer: chart type '{chartTypeName}' is not supported by this renderer — no chart will be shown")]
    public static partial IGenericMessage UnsupportedChartType(ILogger logger, string chartTypeName);

    [MessageLogging(
        EventId = 4773,
        Level = LogLevel.Error,
        Message = "EChartsRenderer: failed to load JS interop module")]
    public static partial IGenericMessage ModuleLoadFailed(ILogger logger, Exception exception);

    [MessageLogging(
        EventId = 4774,
        Level = LogLevel.Error,
        Message = "EChartsRenderer: JS render call failed for chart type '{chartTypeName}'")]
    public static partial IGenericMessage RenderFailed(ILogger logger, Exception exception, string chartTypeName);

    [MessageLogging(
        EventId = 4775,
        Level = LogLevel.Trace,
        Message = "EChartsRenderer: JS interop interrupted during teardown/navigation (expected)")]
    public static partial IGenericMessage TeardownInterrupted(ILogger logger, Exception exception);
}
