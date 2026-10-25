using System;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// One sign-in of an account, as an administrator reads it (FDW-2729).
/// </summary>
public class SignInResponse
{
    /// <summary>Gets or sets when the person signed in.</summary>
    public DateTimeOffset SignedInAt { get; set; }

    /// <summary>Gets or sets the Session authentication service the sign-in was issued into; null when the flow issued a bearer token.</summary>
    public string? ServiceName { get; set; }

    /// <summary>Gets or sets the flow that authenticated the person, or the credential kind for a key request.</summary>
    public string FlowName { get; set; } = string.Empty;

    /// <summary>Gets or sets the address the request came from; null when it was not known.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Gets or sets the browser the User-Agent names; null when it names none of the known browsers.</summary>
    public string? Browser { get; set; }

    /// <summary>Gets or sets the operating system the User-Agent names; null when it names none of the known systems.</summary>
    public string? OperatingSystem { get; set; }

    /// <summary>Gets or sets the User-Agent header as the caller sent it; null when none was sent.</summary>
    public string? UserAgent { get; set; }
}
