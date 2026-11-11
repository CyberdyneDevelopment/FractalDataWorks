using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Multitenancy.Configuration;

/// <summary>An organization as its list shows it: the four properties every domain has, and its own list fields.</summary>
/// <remarks>Read from <c>tenant.Organization</c> alone, in one query per page.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class OrganizationDomainConfiguration : DomainConfiguration
{
    /// <summary>Gets or sets the tenant the organization belongs to.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the URL slug, unique within the tenant.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this is the tenant's default organization: the one a request uses when it names none.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Gets or sets whether this is the tenant's admin organization.</summary>
    public bool IsAdminOrg { get; set; }

    /// <summary>Gets or sets whether the organization is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the durable id of this organization's own default workspace (FDW-3668). Set when the organization was founded
    /// with the setting <c>FoundingMakesDefaultWorkspace</c> on; <see langword="null"/> when it has none (FDW-4600).
    /// </summary>
    public Guid? DefaultWorkspaceId { get; set; }
}
