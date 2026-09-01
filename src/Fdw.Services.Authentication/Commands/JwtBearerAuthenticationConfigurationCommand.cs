using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Authentication.Validation;

namespace Fdw.Services.Authentication.Commands;

/// <summary>
/// Reads and writes the JwtBearer implementation rows of <c>auth.AuthenticationService</c>.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "JwtBearerAuthenticationService")]
public sealed class JwtBearerAuthenticationConfigurationCommand
    : ConfigurationCommandBase<JwtBearerAuthenticationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="JwtBearerAuthenticationConfigurationCommand"/> class.</summary>
    public JwtBearerAuthenticationConfigurationCommand()
        : base("JwtBearerAuthenticationService")
    {
    }
}
