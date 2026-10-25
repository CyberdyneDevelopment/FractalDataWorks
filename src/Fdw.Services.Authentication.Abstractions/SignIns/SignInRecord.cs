using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Abstractions.SignIns;

/// <summary>
/// One recorded sign-in of a person, as an administrator reads it (FDW-2729).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record SignInRecord
{
    /// <summary>Gets when the person signed in.</summary>
    public DateTimeOffset SignedInAt { get; init; }

    /// <summary>Gets the Session authentication service the sign-in was issued into; null when the flow issued a bearer token.</summary>
    public string? ServiceName { get; init; }

    /// <summary>Gets the flow that authenticated the person, or the credential kind for a key request.</summary>
    public string FlowName { get; init; } = string.Empty;

    /// <summary>Gets the address the request came from; null when it was not known.</summary>
    public string? IpAddress { get; init; }

    /// <summary>Gets the User-Agent header as sent; null when none was sent.</summary>
    public string? UserAgent { get; init; }

    /// <summary>Gets the browser the User-Agent names; null when it names none of the known browsers.</summary>
    public string? Browser { get; init; }

    /// <summary>Gets the operating system the User-Agent names; null when it names none of the known systems.</summary>
    public string? OperatingSystem { get; init; }
}
