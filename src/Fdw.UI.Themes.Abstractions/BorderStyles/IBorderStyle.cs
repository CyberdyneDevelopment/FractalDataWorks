using Fdw.Collections;
using Spectre.Console;

namespace Fdw.UI.Themes;

/// <summary>
/// Defines border styles for UI components.
/// </summary>
public interface IBorderStyle : ITypeOption<int, BorderStyleBase>
{
    /// <summary>Default panel border style.</summary>
    BoxBorder Panel { get; }

    /// <summary>Input field border style.</summary>
    BoxBorder Input { get; }

    /// <summary>Menu border style.</summary>
    BoxBorder Menu { get; }

    /// <summary>Dialog/modal border style.</summary>
    BoxBorder Dialog { get; }

    /// <summary>Table border style.</summary>
    TableBorder Table { get; }

    /// <summary>Selection list border style.</summary>
    BoxBorder Selection { get; }
}
