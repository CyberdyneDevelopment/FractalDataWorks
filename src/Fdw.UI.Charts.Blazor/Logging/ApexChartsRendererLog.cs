#pragma warning disable CS1591
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Charts.Blazor.Logging;

/// <summary>
/// MessageLogging for <c>ApexChartsRenderer</c> operations.
/// EventId range: 4740–4749 (UI charts renderer layer).
/// </summary>
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("CHARTS")]
public static partial class ApexChartsRendererLog
{
    [MessageLogging(
        EventId = 4740,
        Level = LogLevel.Trace,
        Message = "ApexChartsRenderer: beginning render for chart type '{chartTypeName}' with {rowCount} row(s)")]
    public static partial IGenericMessage RenderBegin(ILogger logger, string chartTypeName, int rowCount);

    [MessageLogging(
        EventId = 4741,
        Level = LogLevel.Information,
        Message = "ApexChartsRenderer: rendered chart type '{chartTypeName}' with {rowCount} row(s)")]
    public static partial IGenericMessage Rendered(ILogger logger, string chartTypeName, int rowCount);

    [MessageLogging(
        EventId = 4742,
        Level = LogLevel.Warning,
        Message = "ApexChartsRenderer: chart type '{chartTypeName}' is not supported by this renderer — no chart will be shown")]
    public static partial IGenericMessage UnsupportedChartType(ILogger logger, string chartTypeName);
}
