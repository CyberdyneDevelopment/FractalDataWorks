using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Identity.OpenIddict.Commands;

/// <summary>ConfigurationCommands TypeOption for the FDW OpenIddict typed body (sec.OpenIddictIdentity).</summary>
[TypeOption(typeof(ConfigurationCommands), "OpenIddictIdentity")]
public sealed class OpenIddictConfigurationCommand : ConfigurationCommandBase<OpenIddictConfiguration>
{
    /// <inheritdoc/>
    public OpenIddictConfigurationCommand() : base("OpenIddictIdentity") { }
}
