using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Models;

/// <summary>
/// Configuration options for support contacts.
/// Bind from appsettings.json "Support" section.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public sealed class SupportOptions
{
    public string Email { get; set; } = "";

    public string? Phone { get; set; }

    public string? PortalUrl { get; set; }

    public int ExpectedResponseTimeHours { get; set; } = 24;

    public string Instructions { get; set; } = "If this error persists, please contact support with the Request ID above.";
}
