namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// What the ApiKey kind needs to validate an opaque credential this host minted itself.
/// </summary>
/// <remarks>
/// The typed contract for one implementation of the authentication-service domain. Carries nothing
/// beyond the base contract: an agent key or personal access token names no issuer and no audience —
/// it is a lookup key, and everything else the row would otherwise carry (<c>Authority</c>) is never
/// read by <c>ApiKeyAuthenticationType.TakeScheme</c>. A row still exists so the domain has something
/// to dispatch to when it names the <c>ApiKey</c> kind.
/// </remarks>
public interface IApiKeyAuthenticationConfiguration : IAuthenticationServiceImplementationConfiguration
{
}
