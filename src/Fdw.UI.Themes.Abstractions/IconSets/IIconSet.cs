using Fdw.Collections;

namespace Fdw.UI.Themes;

/// <summary>
/// Defines icons and indicators for UI components.
/// Icons are string-based to support Unicode, ASCII, or emoji indicators.
/// </summary>
public interface IIconSet : ITypeOption<int, IconSetBase>
{
    /// <summary>Indicator for selected items (e.g., ">" or "\u25cf").</summary>
    string SelectedIndicator { get; }

    /// <summary>Indicator for unselected items (e.g., " " or "\u25cb").</summary>
    string UnselectedIndicator { get; }

    /// <summary>Indicator for checked items (e.g., "[x]" or "\u2611").</summary>
    string CheckedIndicator { get; }

    /// <summary>Indicator for unchecked items (e.g., "[ ]" or "\u2610").</summary>
    string UncheckedIndicator { get; }

    /// <summary>Indicator for required fields (e.g., "*").</summary>
    string RequiredIndicator { get; }

    /// <summary>Icon for success status (e.g., "\u2713" or "[OK]").</summary>
    string SuccessIcon { get; }

    /// <summary>Icon for error status (e.g., "\u2717" or "[ERR]").</summary>
    string ErrorIcon { get; }

    /// <summary>Icon for warning status (e.g., "\u26a0" or "[WARN]").</summary>
    string WarningIcon { get; }

    /// <summary>Icon for info status (e.g., "\u2139" or "[INFO]").</summary>
    string InfoIcon { get; }

    /// <summary>Icon for expanded tree nodes (e.g., "\u25bc" or "[-]").</summary>
    string ExpandedIcon { get; }

    /// <summary>Icon for collapsed tree nodes (e.g., "\u25b6" or "[+]").</summary>
    string CollapsedIcon { get; }

    /// <summary>Icon for loading/processing (e.g., "\u231b" or "[...]").</summary>
    string LoadingIcon { get; }
}
