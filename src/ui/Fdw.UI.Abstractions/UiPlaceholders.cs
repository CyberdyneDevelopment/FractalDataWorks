using Fdw.Conventions;

namespace Fdw.UI.Abstractions;

/// <summary>
/// The texts a terminal or console screen shows where a value is absent. None of them is ever a valid value of what it stands for (FDW-3580).
/// </summary>
/// <remarks>
/// Why declared here: FDW055 accepts a placeholder only as a [PlaceholderText] constant, and the renderers and TUI components each wrote their
/// own literal. The wording each used is kept; the Spectre renderers and the TUI components both reference this project.
/// </remarks>
[PlaceholderText]
public static class UiPlaceholders
{
    /// <summary>Stands for a model value that is null, as the TUI components show it (dimmed markup).</summary>
    public const string NullValueMarkup = "[dim]null[/]";

    /// <summary>Stands for a table cell or a row whose value is absent.</summary>
    public const string NoValue = "-";

    /// <summary>Stands for an input or display field that holds no value.</summary>
    public const string EmptyValue = "(empty)";

    /// <summary>Stands for a select that has no selected option.</summary>
    public const string NoSelection = "(none)";

    /// <summary>Stands for the start of a log range that states none.</summary>
    public const string OpenRangeStart = "...";

    /// <summary>Stands for the end of a log range that states none, which is the present.</summary>
    public const string OpenRangeEnd = "now";

    /// <summary>Stands for a wizard step that states no description.</summary>
    public const string NoStepDescription = "Complete the fields below.";
}
