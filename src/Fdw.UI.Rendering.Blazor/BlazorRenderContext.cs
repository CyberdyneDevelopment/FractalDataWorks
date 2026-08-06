using System;
using System.Collections.Generic;
using Fdw.UI.Abstractions.RenderModeOptions;
using Fdw.UI.Abstractions.Rendering;
using Microsoft.AspNetCore.Components;

namespace Fdw.UI.Rendering.Blazor;

/// <summary>
/// Render context for the Blazor rendering backend.
/// </summary>
/// <remarks>
/// Unlike the imperative console context, a Blazor context is a fragment sink: the renderer
/// composes <see cref="RenderFragment"/>s into it and the hosting component re-renders when
/// <see cref="StateChanged"/> fires. Console-flavored members are absent by construction.
/// </remarks>
public sealed class BlazorRenderContext : IRenderContext
{
    private readonly List<RenderFragment> _fragments = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorRenderContext"/> class.
    /// </summary>
    /// <param name="mode">The render mode (Display, Edit, ReadOnly).</param>
    /// <param name="theme">Optional theme object (e.g. a CSS theme name).</param>
    public BlazorRenderContext(IRenderMode mode, object? theme = null)
    {
        Mode = mode ?? throw new ArgumentNullException(nameof(mode));
        Theme = theme;
        Data = new Dictionary<string, object>(StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public IRenderMode Mode { get; }

    /// <inheritdoc />
    public object? Theme { get; }

    /// <inheritdoc />
    // Why: the neutral contract carries console-flavored dimension members; a Blazor surface
    // has no fixed character grid, so these are null by construction rather than guessed.
    public int? ConsoleWidth => null;

    /// <inheritdoc />
    public int? ConsoleHeight => null;

    /// <inheritdoc />
    public bool SupportsUnicode => true;

    /// <inheritdoc />
    public IDictionary<string, object> Data { get; }

    /// <summary>
    /// Gets the fragments produced by <c>Render</c> calls, in call order.
    /// </summary>
    public IReadOnlyList<RenderFragment> Fragments => _fragments;

    /// <summary>
    /// Gets the fragment produced by the most recent <c>RenderPage</c> or <c>Prompt</c> call.
    /// </summary>
    public RenderFragment? ActiveFragment { get; private set; }

    /// <summary>
    /// Raised when the renderer changes the context content and the host should re-render.
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// Adds a display fragment produced by <c>Render</c>.
    /// </summary>
    /// <param name="fragment">The fragment to add.</param>
    internal void AddFragment(RenderFragment fragment)
    {
        _fragments.Add(fragment);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets the interactive fragment produced by <c>RenderPage</c> or <c>Prompt</c>.
    /// </summary>
    /// <param name="fragment">The fragment to activate.</param>
    internal void SetActiveFragment(RenderFragment fragment)
    {
        ActiveFragment = fragment;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Requests a host re-render after in-fragment state changes (e.g. validation messages).
    /// </summary>
    internal void NotifyStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}
