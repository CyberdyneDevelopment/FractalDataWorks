using Fdw.Services.Abstractions;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Reads the JwtBearer rows of <c>auth.AuthenticationService</c>.
/// </summary>
public interface IJwtBearerAuthenticationConfigurationProvider
    : IImplementationConfigurationProvider<IJwtBearerAuthenticationConfiguration>
{
}
