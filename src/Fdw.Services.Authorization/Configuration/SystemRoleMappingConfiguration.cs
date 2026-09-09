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
public sealed partial class SystemRoleMappingConfiguration : IRoleMappingImplementationConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    // Why the name is not persisted here: an implementation is identified by its reference to the
    // domain row, and the name comes from RoleMappingConfiguration.Name. The setter exists so the
    // domain can copy it across for logging once the two are composed.
    /// <summary>Gets or sets the name, copied from the domain record after composition.</summary>
    string IGenericConfiguration.Name { get; set; } = string.Empty;

    /// <summary>Gets the configuration section this implementation belongs to.</summary>
    string IGenericConfiguration.SectionName => "RoleMapping";

    /// <summary>Gets the service category this implementation belongs to.</summary>
    string IGenericConfiguration.ServiceType => "RoleMapping";

    // Why settable rather than a constant: the discriminator is data. The domain row names the
    // implementation and the row carries the value; a hardcoded literal here would state the same
    // fact a second time, by hand, and is what let configurations declare a discriminator they had
    // no claim to.
    /// <summary>Gets or sets the option name this implementation was selected by.</summary>
    public string? ServiceOptionType { get; set; }

    /// <summary>Gets or sets the foreign key to the owning <see cref="RoleMappingConfiguration"/> row.</summary>
    public Guid RoleMappingId { get; set; }

    /// <summary>Gets or sets the role name that grants administrator authority.</summary>
    public string? AdminRoleName { get; set; }

    /// <summary>Gets or sets the role name that grants operator authority.</summary>
    public string? OperatorRoleName { get; set; }

    /// <summary>Gets or sets the role name that grants read-only authority.</summary>
    public string? ViewerRoleName { get; set; }
}
