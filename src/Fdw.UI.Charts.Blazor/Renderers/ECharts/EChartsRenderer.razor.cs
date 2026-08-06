using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.UI.Abstractions.Charts;
using Fdw.UI.Charts.Blazor.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace Fdw.UI.Charts.Blazor.Renderers.ECharts;

/// <summary>
/// Blazor renderer component for ECharts via JS interop.
/// </summary>
/// <remarks>
/// <para>
/// Renders the <see cref="Model"/> spec against the provided <see cref="Rows"/> using the
/// Apache ECharts library loaded from the vendored UMD bundle. Chart-type dispatch is
/// data-driven through <see cref="EChartsStrategyMap"/> — no switch/if-else chain on the
/// chart type name (FDW019).
/// </para>
/// <para>
/// This component is mounted exclusively by <c>ChartHost</c> via <c>DynamicComponent</c>.
/// No <c>@page</c> directive — it is not a routable page.
/// </para>
/// <para>
/// JS interop uses an ES module (<c>echarts-interop.js</c>) that lazily loads the vendored
/// ECharts UMD bundle on first call and caches the chart instance per DOM element.
/// </para>
/// </remarks>
[SuppressMessage("Meziantou.Analyzer", "MA0004",
    Justification = "Blazor UI event handlers run on sync context")]
public sealed partial class EChartsRenderer : ComponentBase, IAsyncDisposable
{
    // ── Parameters ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the render-agnostic chart model. Required.
    /// </summary>
    [Parameter, EditorRequired]
    public IChartModel Model { get; set; } = default!;

    /// <summary>
    /// Gets or sets the data rows to plot.
    /// </summary>
    /// <remarks>
    /// Each row is a field-name-to-value dictionary. The renderer extracts values by the
    /// field names declared in <see cref="IChartModel.Encodings"/>.
    /// </remarks>
    [Parameter]
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; set; } = [];

    /// <summary>
    /// Gets or sets the optional logger. Falls back to <see cref="NullLogger{T}.Instance"/>.
    /// </summary>
    // Why: NullLogger fallback is the only acceptable ?? fallback per FDW conventions.
    [Parameter]
    public ILogger? Logger { get; set; }

    // ── Injected ──────────────────────────────────────────────────────────────────

    // Why: IJSRuntime is injected by the Blazor DI container; the JS interop module is
    // lazily imported on first render so the module file is not fetched until the chart
    // component is actually mounted.
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    // ── Private state ─────────────────────────────────────────────────────────────

    private IJSObjectReference? _module;
    private ElementReference _el;
    private string? _errorMessage;
    private Dictionary<string, object?>? _option;
    private bool _needsRender;
    private bool _disposed;

    private ILogger ResolvedLogger => Logger ?? NullLogger<EChartsRenderer>.Instance;

    // ── Lifecycle ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Model is null)
            return;

        EChartsRendererLog.RenderBegin(ResolvedLogger, Model.ChartType.Name, Rows.Count);

        var strategy = EChartsStrategyMap.For(Model.ChartType.Name);
        if (strategy is null)
        {
            _errorMessage = EChartsRendererLog.UnsupportedChartType(ResolvedLogger, Model.ChartType.Name).Message;
            _option      = null;
            _needsRender = false;
            return;
        }

        _errorMessage = null;
        _option      = strategy(Model, Rows);
        _needsRender = true;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
            return;

        if (firstRender)
        {
            try
            {
                _module = await JS.InvokeAsync<IJSObjectReference>(
                    "import",
                    "./_content/Fdw.UI.Charts.Blazor/js/echarts-interop.js");
            }
            catch (OperationCanceledException ex)
            {
                EChartsRendererLog.TeardownInterrupted(ResolvedLogger, ex);
                return;
            }
            catch (Exception ex)
            {
                _errorMessage = EChartsRendererLog.ModuleLoadFailed(ResolvedLogger, ex).Message;
                StateHasChanged();
                return;
            }
        }

        if (_module is not null && _needsRender && _option is not null)
        {
            _needsRender = false;
            try
            {
                await _module.InvokeVoidAsync("render", _el, _option);
                EChartsRendererLog.Rendered(ResolvedLogger, Model.ChartType.Name, Rows.Count);
            }
            catch (OperationCanceledException ex)
            {
                // Why: cancellation during dispose or navigation is expected — observe at Trace, no error state.
                EChartsRendererLog.TeardownInterrupted(ResolvedLogger, ex);
            }
            catch (Exception ex)
            {
                _errorMessage = EChartsRendererLog.RenderFailed(ResolvedLogger, ex, Model.ChartType.Name).Message;
                StateHasChanged();
            }
        }
    }

    // ── IAsyncDisposable ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_module is not null)
        {
            try
            {
                // Why: notify the JS module to dispose the ECharts instance before releasing
                // the JS object reference, so ECharts can clean up its internal event listeners
                // and DOM mutations without leaking memory.
                await _module.InvokeVoidAsync("dispose", _el);
            }
            catch (JSDisconnectedException ex) { EChartsRendererLog.TeardownInterrupted(ResolvedLogger, ex); }
            catch (OperationCanceledException ex) { EChartsRendererLog.TeardownInterrupted(ResolvedLogger, ex); }

            await _module.DisposeAsync();
            _module = null;
        }
    }
}
