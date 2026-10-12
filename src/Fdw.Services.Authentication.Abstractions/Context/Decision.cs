using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>
/// Whether this principal may be issued a token, and why.
/// </summary>
/// <remarks>
/// This is the login-time question — is the account enabled, the tenant active, the client consented.
/// It is not the per-request question of whether a principal may perform some action on some object;
/// that happens thousands of times per second against a live decision point and is never part of a
/// flow. The flow ends when a token is issued.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record Decision
{
    /// <summary>Gets a value indicating whether issuance is permitted.</summary>
    public required bool Permitted { get; init; }

    /// <summary>Gets the reason, which a denial must always carry.</summary>
    public required string Reason { get; init; }
}
