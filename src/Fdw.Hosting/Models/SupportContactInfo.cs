using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Models;

/// <summary>
/// Contact information for support escalation.
/// </summary>
// Why: pure DTO, only auto-properties, no logic.
[ExcludeFromCodeCoverage]
public sealed class SupportContactInfo
{
    public string Email { get; set; } = "";

    public string? Phone { get; set; }

    public string? PortalUrl { get; set; }

    public string Instructions { get; set; } = "If this error persists, please contact support with the Request ID above.";

    public int? ExpectedResponseTimeHours { get; set; }
}
