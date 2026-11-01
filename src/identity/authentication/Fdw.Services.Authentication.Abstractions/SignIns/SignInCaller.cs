using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Abstractions.SignIns;

/// <summary>
/// Who was calling when a person signed in (FDW-2729): the address the request came from and the User-Agent it sent.
/// </summary>
/// <remarks>
/// Each part is null when the caller did not give it. That is a state the log records honestly: a key used from a script sends no browser, and a
/// host with no request (a TDS login) has no address. Nothing is filled in on the caller's behalf.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record SignInCaller
{
    /// <summary>Gets the caller that is not known: no address and no User-Agent.</summary>
    public static SignInCaller NotKnown { get; } = new();

    /// <summary>Gets the address the request came from, as text; null when it is not known.</summary>
    public string? IpAddress { get; init; }

    /// <summary>Gets the User-Agent header as the caller sent it; null when the caller sent none.</summary>
    public string? UserAgent { get; init; }
}
