using Fdw.Collections;
using Spectre.Console;

namespace Fdw.UI.Themes;

/// <summary>
/// Defines a color palette for UI components.
/// </summary>
/// <remarks>
/// <para>
/// Colors are organized into categories:
/// <list type="bullet">
/// <item><description>Primary/Secondary: Main UI accent colors</description></item>
/// <item><description>Background/Foreground: Base colors</description></item>
/// <item><description>Semantic: Success, Warning, Error, Info</description></item>
/// <item><description>Interactive: Focused, Selected, Disabled, Hover</description></item>
/// <item><description>Component-specific: Input borders, table headers, etc.</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IColorPalette : ITypeOption<int, ColorPaletteBase>
{
    /// <summary>Primary accent color.</summary>
    Color Primary { get; }

    /// <summary>Secondary accent color.</summary>
    Color Secondary { get; }

    /// <summary>Background color.</summary>
    Color Background { get; }

    /// <summary>Foreground (text) color.</summary>
    Color Foreground { get; }

    /// <summary>Muted foreground color for less important text.</summary>
    Color Muted { get; }

    /// <summary>Success indicator color.</summary>
    Color Success { get; }

    /// <summary>Warning indicator color.</summary>
    Color Warning { get; }

    /// <summary>Error indicator color.</summary>
    Color Error { get; }

    /// <summary>Informational indicator color.</summary>
    Color Info { get; }

    /// <summary>Color for focused elements.</summary>
    Color Focused { get; }

    /// <summary>Color for selected elements.</summary>
    Color Selected { get; }

    /// <summary>Color for disabled elements.</summary>
    Color Disabled { get; }

    /// <summary>Color for hovered elements.</summary>
    Color Hover { get; }

    /// <summary>Default input border color.</summary>
    Color InputBorder { get; }

    /// <summary>Input border color when focused.</summary>
    Color InputBorderFocused { get; }

    /// <summary>Input border color when there's an error.</summary>
    Color InputBorderError { get; }

    /// <summary>Table header background color.</summary>
    Color TableHeader { get; }

    /// <summary>Alternate row background color for tables.</summary>
    Color TableRowAlternate { get; }
}
