namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// What the ApiKey kind needs to validate an opaque credential this host minted itself.
/// </summary>
/// <remarks>
/// The typed contract for one implementation of the authentication-service domain. An agent key or
/// personal access token names no issuer and no audience — it is a lookup key, so <c>Authority</c> is
/// never read by <c>ApiKeyAuthenticationType.TakeScheme</c>. What the row does carry is who may use a
/// key of each kind: a key acts as its owner, so the owner must hold the kind's required permission
/// in the tenant the key acts in, exactly as a gated sign-in flow requires of a person signing in.
/// </remarks>
public interface IApiKeyAuthenticationConfiguration : IAuthenticationServiceImplementationConfiguration
{
    /// <summary>
    /// Gets or sets the permission the owner of a personal access token must hold for the token to
    /// authenticate; null when every active user's tokens authenticate.
    /// </summary>
    string? RequiredUserKeyPermission { get; set; }

    /// <summary>
    /// Gets or sets the permission the owner of an agent key must hold for the key to authenticate;
    /// null when every active user's agent keys authenticate.
    /// </summary>
    string? RequiredAgentKeyPermission { get; set; }
}
