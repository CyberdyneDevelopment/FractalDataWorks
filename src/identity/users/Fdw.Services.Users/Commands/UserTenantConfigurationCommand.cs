using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Users.Configuration;

namespace Fdw.Services.Users.Commands;

/// <summary>ConfigurationCommands TypeOption for the UserTenant configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "UserTenant")]
public sealed class UserTenantConfigurationCommand : ConfigurationCommandBase<UserTenantImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="UserTenantConfigurationCommand"/> class.</summary>
    public UserTenantConfigurationCommand() : base("UserTenants") { }
}
