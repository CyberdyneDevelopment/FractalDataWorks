using System;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Response for a department switch (FDW-1802).
/// </summary>
public class SwitchOrganizationDto
{
    /// <summary>Gets or sets a value indicating whether the switch was made.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the new access token, carrying the department; empty for a caller signed in by a session.</summary>
    public string? AccessToken { get; set; }

    /// <summary>Gets or sets when the access token expires.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Gets or sets the department the caller now acts in.</summary>
    public OrganizationSummaryResponse? Organization { get; set; }
}
