using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Authentication.Validation;

namespace Fdw.Services.Authentication.Commands;

/// <summary>
/// Reads and writes the LocalKey implementation rows of <c>auth.AuthenticationService</c>.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "LocalKeyAuthentication")]
public sealed class LocalKeyAuthenticationConfigurationCommand
    : ConfigurationCommandBase<LocalKeyAuthenticationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="LocalKeyAuthenticationConfigurationCommand"/> class.</summary>
    public LocalKeyAuthenticationConfigurationCommand()
        : base("LocalKeyAuthentication")
    {
    }
}
