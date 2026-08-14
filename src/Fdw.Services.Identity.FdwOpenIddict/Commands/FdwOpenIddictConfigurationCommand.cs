using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Identity.FdwOpenIddict.Commands;

/// <summary>ConfigurationCommands TypeOption for the FDW OpenIddict typed body (sec.FdwOpenIddictIdentity).</summary>
[TypeOption(typeof(ConfigurationCommands), "FdwOpenIddictIdentity")]
public sealed class FdwOpenIddictConfigurationCommand : ConfigurationCommandBase<FdwOpenIddictConfiguration>
{
    /// <inheritdoc/>
    public FdwOpenIddictConfigurationCommand() : base("FdwOpenIddictIdentity") { }
}
