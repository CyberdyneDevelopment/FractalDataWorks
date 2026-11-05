using Fdw.Conventions;

namespace Fdw.TUI.Management;

/// <summary>
/// The text the management console shows where a value is absent (FDW-3580). None is ever a valid value of what it stands in for, so
/// FDW055 accepts it only as one of these constants, never as a bare literal.
/// </summary>
[PlaceholderText]
public static class ScreenPlaceholders
{
    /// <summary>Shown for an instance, a status or a name the server did not report.</summary>
    public const string Unknown = "Unknown";

    /// <summary>Shown for a URL or a value that does not apply.</summary>
    public const string NotApplicable = "N/A";

    /// <summary>Shown for a time that has not happened yet, such as a data store never discovered.</summary>
    public const string Never = "Never";

    /// <summary>Shown for a schedule with no next run.</summary>
    public const string NoNextRun = "—";

    /// <summary>Names a test run that covers every folder of the suite.</summary>
    public const string WholeSuite = "the whole suite";

    /// <summary>Shown for a Newman request with no name.</summary>
    public const string UnnamedRequest = "(unnamed request)";
}
