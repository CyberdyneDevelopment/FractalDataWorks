namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// What the JwtBearer kind needs to check a token another issuer signed.
/// </summary>
/// <remarks>
/// The typed contract for the remote-issuer implementation. Unlike LocalKey it names roles, because
/// a foreign issuer's roles are the ones this host chooses to honour.
/// </remarks>
public interface IJwtBearerAuthenticationConfiguration : IAuthenticationServiceImplementationConfiguration
{
    /// <summary>Gets or sets the audience a token must name.</summary>
    string Audience { get; set; }

    /// <summary>Gets or sets the roles this host honours from the issuer's tokens.</summary>
    /// <remarks>
    /// One column, the role names separated by commas, which is how a configuration row carries a
    /// list here — <c>sec.ClientCredentialsIdentity.Scopes</c> and <c>sec.JwtAssertionIdentity.Scopes</c>
    /// are the same shape. Every role table in the schema holds authorization data rather than
    /// configuration, so a child table would be a different thing wearing a similar name.
    /// </remarks>
    string Roles { get; set; }
}
