using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Multitenancy.Configuration;

/// <summary>
/// A row of <c>tenant.Organization</c> as the organization lookup reads it (FDW-4573): the columns <see cref="Fdw.Services.Multitenancy.Abstractions.OrganizationConfiguration"/> carries.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class OrganizationRow
{
    /// <summary>Gets or sets the organization's durable Id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the tenant the organization belongs to.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the organization's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the organization's slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether this is the tenant's default organization.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Gets or sets a value indicating whether this is an administrative organization.</summary>
    public bool IsAdminOrg { get; set; }

    /// <summary>Gets or sets a value indicating whether the organization is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the organization's default workspace; null when it has none.</summary>
    public Guid? DefaultWorkspaceId { get; set; }

    /// <summary>Gets or sets the visibility group of the row.</summary>
    public Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets a value indicating whether this is the current version.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Gets or sets a value indicating whether the row is deleted.</summary>
    public bool IsDeleted { get; set; }
}
