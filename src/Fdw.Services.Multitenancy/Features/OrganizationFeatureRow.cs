using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// A row of the table of features a tenant switched on for an organization (FDW-1580).
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class OrganizationFeatureRow
{
    /// <summary>Gets or sets the row's identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the tenant the organization belongs to.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the organization the feature is switched on for.</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>Gets or sets the feature's name in the catalogue.</summary>
    public string Feature { get; set; } = string.Empty;
}
