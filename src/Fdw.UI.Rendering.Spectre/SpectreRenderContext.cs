using System;
using System.Collections.Generic;
using Fdw.UI.Abstractions.RenderModeOptions;
using Fdw.UI.Abstractions.Rendering;
using Fdw.UI.Themes;
using Spectre.Console;

namespace Fdw.UI.Rendering.Spectre;

/// <summary>
/// Render context for Spectre.Console rendering.
/// </summary>
public sealed class SpectreRenderContext : IRenderContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpectreRenderContext"/> class.
    /// </summary>
    /// <param name="console">The Spectre.Console instance.</param>
    /// <param name="theme">The theme to use; the host picks it from its configuration.</param>
    public SpectreRenderContext(IAnsiConsole? console, IMenuTheme theme)
    {
        Console = console ?? AnsiConsole.Console;
        Theme = theme ?? throw new ArgumentNullException(nameof(theme));
    }

    /// <summary>
    /// Gets the Spectre.Console instance.
    /// </summary>
    public IAnsiConsole Console { get; }

    /// <inheritdoc />
    public IRenderMode Mode { get; set; } = RenderModes.Edit;

    /// <inheritdoc />
    object? IRenderContext.Theme => Theme;

    /// <summary>
    /// Gets the typed theme.
    /// </summary>
    public IMenuTheme Theme { get; }

    /// <inheritdoc />
    public int? ConsoleWidth => Console.Profile.Width;

    /// <inheritdoc />
    public int? ConsoleHeight => Console.Profile.Height;

    /// <inheritdoc />
    public bool SupportsUnicode => Console.Profile.Capabilities.Unicode;

    /// <inheritdoc />
    public IDictionary<string, object> Data { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <summary>
    /// Gets a value indicating whether the console supports ANSI.
    /// </summary>
    public bool SupportsAnsi => Console.Profile.Capabilities.Ansi;

    /// <summary>
    /// Gets a value indicating whether the console supports links.
    /// </summary>
    public bool SupportsLinks => Console.Profile.Capabilities.Links;
}
