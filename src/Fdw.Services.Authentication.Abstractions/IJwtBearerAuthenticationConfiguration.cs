using System.Collections.Generic;

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
    IReadOnlyList<string> Roles { get; set; }
}
