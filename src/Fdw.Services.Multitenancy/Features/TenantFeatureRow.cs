using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// A row of the table of features the platform switched on for a tenant (FDW-1580).
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class TenantFeatureRow
{
    /// <summary>Gets or sets the row's identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the tenant the feature is switched on for.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the feature's name in the catalogue.</summary>
    public string Feature { get; set; } = string.Empty;
}
