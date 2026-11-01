using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Multitenancy.Configuration;

/// <summary>
/// Database-backed configuration for an organization: a unit of a tenant, such as a school of a district.
/// Maps to <c>tenant.OrganizationConfiguration</c>, keyed to its domain row in <c>tenant.Organization</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Organization")]
public partial class OrganizationImplementationConfiguration
    : IOrganizationImplementationConfiguration
{
    /// <inheritdoc/>
    /// <remarks>Stamped by the provider from the domain row.</remarks>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;
}
