namespace Fdw.VsCodeShell.Hosting;

/// <summary>
/// Required identity fields a host extension supplies via <c>AddVsCodeShell(...)</c>.
/// These appear in the manifest the bootstrap reads at startup.
/// </summary>
public sealed class VsCodeShellOptions
{
    /// <summary>Extension identifier (e.g. <c>fractaldataworks.pidgin-canvas</c>).</summary>
    public string ExtensionId { get; set; } = string.Empty;

    /// <summary>Human-readable extension name.</summary>
    public string DisplayName { get; set; } = string.Empty;
}
