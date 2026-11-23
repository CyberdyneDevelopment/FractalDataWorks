using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Multitenancy.Configuration;

/// <summary>
/// A row of <c>tenant.UserTenantsConfiguration</c> as the single-tenant option's tenant lookup reads it (FDW-6528): a person's membership of a tenant. The domain
/// table carries only the member's name; the implementation table, this one, carries the membership (FDW-825).
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class UserTenantRow
{
    /// <summary>Gets or sets the person.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the tenant the person is a member of.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets a value indicating whether this is the current version of the membership; a removed membership is a revoked one.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Gets or sets a value indicating whether the row is deleted.</summary>
    public bool IsDeleted { get; set; }
}
