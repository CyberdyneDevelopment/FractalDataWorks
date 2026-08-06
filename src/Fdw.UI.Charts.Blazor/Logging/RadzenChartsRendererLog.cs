#pragma warning disable CS1591
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Charts.Blazor.Logging;

/// <summary>
/// MessageLogging for <c>RadzenChartsRenderer</c> operations.
/// EventId range: 4750–4759 (UI charts — Radzen renderer layer).
/// </summary>
/// <remarks>
/// EventIds 4730–4739 were the initial candidate range but the band 4720–4739 is allocated
/// to ChartHostLog per EVENTID-ALLOCATION.md. This class uses 4750–4759, the first free
/// block after the ApexCharts renderer (4740–4749).
/// </remarks>
// Why: default TypeCode ("FDW") collided with Fdw.UI.Canvas.Blazor's BlazorDiagramsRendererLog,
// which independently reused EventIds 4750-4752 — both generated "FDW-4750"/"FDW-4751"/"FDW-4752"
// for unrelated messages. A distinct per-project TypeCode makes the generated Code unique even
// though the numeric EventId ranges still overlap across the two sibling UI projects.
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("CHARTS")]
public static partial class RadzenChartsRendererLog
{
    [MessageLogging(
        EventId = 4750,
        Level = LogLevel.Trace,
        Message = "RadzenChartsRenderer: beginning render for chart type '{chartTypeName}' with {rowCount} row(s)")]
    public static partial IGenericMessage RenderBegin(ILogger logger, string chartTypeName, int rowCount);

    [MessageLogging(
        EventId = 4751,
        Level = LogLevel.Information,
        Message = "RadzenChartsRenderer: rendered chart type '{chartTypeName}' with {rowCount} row(s)")]
    public static partial IGenericMessage Rendered(ILogger logger, string chartTypeName, int rowCount);

    [MessageLogging(
        EventId = 4752,
        Level = LogLevel.Warning,
        Message = "RadzenChartsRenderer: chart type '{chartTypeName}' is not supported by this renderer — no chart will be shown")]
    public static partial IGenericMessage UnsupportedChartType(ILogger logger, string chartTypeName);
}
