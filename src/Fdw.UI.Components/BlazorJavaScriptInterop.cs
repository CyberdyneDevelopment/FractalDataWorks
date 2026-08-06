using System.Threading.Tasks;
using Microsoft.JSInterop;
using Fdw.UI.Web.Abstractions;

namespace Fdw.UI.Components;

/// <summary>
/// Blazor implementation of IJavaScriptInterop using IJSRuntime.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0004", Justification = "Blazor JSInterop requires synchronization context for UI updates")]
public sealed class BlazorJavaScriptInterop : IJavaScriptInterop
{
    private readonly IJSRuntime _jsRuntime;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorJavaScriptInterop"/> class.
    /// </summary>
    /// <param name="jsRuntime">The Blazor IJSRuntime instance.</param>
    public BlazorJavaScriptInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc/>
    public async Task<T> Invoke<T>(string identifier, params object[] args)
    {
        return await _jsRuntime.InvokeAsync<T>(identifier, args);
    }

    /// <inheritdoc/>
    public async Task InvokeVoid(string identifier, params object[] args)
    {
        await _jsRuntime.InvokeVoidAsync(identifier, args);
    }
}
