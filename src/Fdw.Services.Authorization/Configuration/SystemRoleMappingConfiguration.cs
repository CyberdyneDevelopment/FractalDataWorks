using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>
/// Which role names carry system authority.
/// </summary>
/// <remarks>
/// Was the authz:SystemRoleMapping appsettings section. A role name is authorization data, and the
/// roles it names are rows on this same store -- so keeping the mapping in a configuration file put
/// half of one fact in a different place from the other half.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Role", ServiceType = "SystemRoleMapping")]
public sealed partial class SystemRoleMappingConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of this configuration row.</summary>
    public string Name { get; set; } = string.Empty;

    string IGenericConfiguration.SectionName => "Role";

    string IGenericConfiguration.ServiceType => "Role";

    string? IGenericConfiguration.ServiceOptionType => "SystemRoleMapping";

    /// <summary>Gets or sets the role name that grants administrator authority.</summary>
    public string? AdminRoleName { get; set; }

    /// <summary>Gets or sets the role name that grants operator authority.</summary>
    public string? OperatorRoleName { get; set; }

    /// <summary>Gets or sets the role name that grants read-only authority.</summary>
    public string? ViewerRoleName { get; set; }
}
