namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Well-known token destination names a claim can be written to. Values match the
/// OAuth/OpenID token names (and OpenIddict's destination constants) so the claim-baking
/// pipeline can apply them verbatim. A claim may target one or both.
/// </summary>
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class TokenDestinations
{
    /// <summary>The access token.</summary>
    public const string AccessToken = "access_token";

    /// <summary>The identity token.</summary>
    public const string IdentityToken = "id_token";
}
