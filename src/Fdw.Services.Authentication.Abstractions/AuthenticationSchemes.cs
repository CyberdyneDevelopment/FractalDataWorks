namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// The authentication scheme names FDW sets on a synthesized principal.
/// </summary>
/// <remarks>
/// Here rather than beside the middleware that sets one, because the scheme is what downstream code
/// reads to tell HOW a caller authenticated, and a name defined next to its writer would be a
/// literal repeated at every reader — free to drift from the one actually stamped on the identity.
/// </remarks>
public static class AuthenticationSchemes
{
    /// <summary>
    /// The scheme stamped on a principal authenticated by a personal access token.
    /// </summary>
    /// <remarks>
    /// A PAT is how a non-interactive caller — an agent acting for its owner — presents itself. It
    /// is the only server-side signal that separates such a caller from the person themselves: the
    /// agent acts on their behalf, so its <c>sub</c> claim IS theirs and the claim cannot tell them
    /// apart.
    /// </remarks>
    public const string PatBearer = "PATBearer";
}
