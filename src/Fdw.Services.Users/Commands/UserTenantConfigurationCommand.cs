using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Users.Configuration;

namespace Fdw.Services.Users.Commands;

/// <summary>ConfigurationCommands TypeOption for the UserTenant configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "UserTenant")]
public sealed class UserTenantConfigurationCommand : ConfigurationCommandBase<UserTenantImplementationConfiguration>
{
    /// <inheritdoc/>
    public UserTenantConfigurationCommand() : base("UserTenants") { }
}
