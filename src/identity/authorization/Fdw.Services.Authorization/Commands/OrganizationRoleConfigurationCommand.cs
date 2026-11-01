using Fdw.Collections.Attributes;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Authorization.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the roles an organization defines (FDW-1593).
/// </summary>
/// <remarks>
/// Why: the configuration save resolves the command for a <see cref="OrganizationRoleImplementationConfiguration"/> via
/// <c>ConfigurationCommands.All()</c>; without this TypeOption defining a role would fail loud.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "OrganizationRole")]
public sealed class OrganizationRoleConfigurationCommand : ConfigurationCommandBase<OrganizationRoleImplementationConfiguration>
{
    /// <inheritdoc/>
    public OrganizationRoleConfigurationCommand() : base("OrganizationRole") { }
}
