using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Authorization.Abstractions;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>
/// Which role names carry system authority.
/// </summary>
/// <remarks>
/// The System implementation of the role-mapping domain. Was the authz:SystemRoleMapping appsettings
/// section. A role name is authorization data, and the roles it names are rows on this same store --
/// so keeping the mapping in a configuration file put half of one fact in a different place from the
/// other half.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "RoleMapping", ServiceType = "System")]
public sealed partial class SystemRoleMappingConfiguration : IRoleMappingImplementationConfiguration, ISystemRoleMappingImplementationConfiguration
{
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    // Why it maps with no column of its own: the name is the domain's, and there is one
    // name for a configured member. The domain provider joins the domain row to this one,
    // and the join's result set is what carries it in.
    public string Name { get; set; } = string.Empty;

    // Why settable rather than a constant: the discriminator is data. The domain row names the
    // implementation and the row carries the value; a hardcoded literal here would state the same
    // fact a second time, by hand, and is what let configurations declare a discriminator they had
    // no claim to.

    /// <summary>Gets or sets the foreign key to the owning <see cref="RoleMappingConfiguration"/> row.</summary>
    public Guid RoleMappingId { get; set; }

    /// <summary>Gets or sets the role name that grants administrator authority.</summary>
    public string? AdminRoleName { get; set; }

    /// <summary>Gets or sets the role name that grants operator authority.</summary>
    public string? OperatorRoleName { get; set; }

    /// <summary>Gets or sets the role name that grants read-only authority.</summary>
    public string? ViewerRoleName { get; set; }
}
