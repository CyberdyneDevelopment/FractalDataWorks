using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Authorization.Abstractions;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>
/// The role-mapping domain configuration: which role-mapping implementation is configured, and its settings.
/// </summary>
/// <remarks>
/// A role mapping answers which role names carry which authority. The System implementation names the
/// roles carrying system authority; another implementation could describe a different authority scope,
/// which is why the mapping is a domain rather than a single record.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "RoleMapping")]
public partial class RoleMappingConfiguration : IRoleMappingConfiguration
{
    // Why no generated default: the store assigns identity. A value minted here reaches Get(id) as a
    // real-looking id matching no row, and the miss reads as a data problem rather than an unsaved record.
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the domain this record belongs to.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Gets or sets the option name selecting which role-mapping implementation is configured.</summary>
    public string? Implementation { get; set; }

    /// <summary>Gets or sets the human-readable description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the configuration of the implementation named by <see cref="Implementation"/>.</summary>
    public IRoleMappingImplementationConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    /// <remarks>
    /// The non-generic view of <see cref="Configuration"/>. The platform service provider reads the
    /// implementation without naming this domain's implementation contract; netstandard2.0 rules out
    /// a default interface implementation, so each domain record states it.
    /// </remarks>
    IImplementationConfiguration? IDomainConfiguration.ImplementationConfiguration
    {
        get => Configuration;
        set => Configuration = (IRoleMappingImplementationConfiguration?)value;
    }

}
