using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Data.Configuration;

/// <summary>
/// The part of a data store's row that says whose it is: its tenant and visibility group (FDW-4459).
/// </summary>
/// <remarks>
/// <see cref="DataStoreDomainConfiguration"/> carries no tenant, and a data store written by an administrator of another tenant must be written as the tenant it
/// belongs to, so the owner is read from the row for itself, as a connection's is (<c>ConnectionTenantConfiguration</c>).
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class DataStoreTenantConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the data store's durable Id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the tenant the data store belongs to.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the visibility group of the row.</summary>
    public Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets a value indicating whether this is the current version.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the row is deleted.</summary>
    public bool IsDeleted { get; set; }
}
