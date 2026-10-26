using System;

namespace Fdw.Services.Users.Abstractions;

/// <summary>One tenant a person is a member of (FDW-2676).</summary>
/// <param name="TenantId">The tenant's id.</param>
/// <param name="IsDefault">True when the tenant is the person's default.</param>
public sealed record HeldTenant(Guid TenantId, bool IsDefault);
