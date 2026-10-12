namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// What the LocalKey kind needs to check a token this host issued.
/// </summary>
/// <remarks>
/// The typed contract for one implementation of the authentication-service domain. An option's own
/// interface is what lets a consumer state which kind it reads: two implementations of a domain
/// satisfy the same domain contract, so a dependency written against that contract alone cannot say
/// which of them it means.
/// </remarks>
public interface ILocalKeyAuthenticationConfiguration : IAuthenticationServiceImplementationConfiguration
{
    /// <summary>Gets or sets the audience a token must name.</summary>
    string Audience { get; set; }
}
