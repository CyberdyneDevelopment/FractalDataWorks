using System;

namespace Fdw.Services.Users.Abstractions;

/// <summary>One tenant a person is a member of (FDW-2676).</summary>
/// <param name="TenantId">The tenant's id.</param>
/// <param name="IsDefault">True when the tenant is the person's default.</param>
/// <param name="DefaultOrganizationId">The department the person lands in when they sign in to this tenant; none when they have marked none (FDW-2938).</param>
public sealed record HeldTenant(Guid TenantId, bool IsDefault, Guid? DefaultOrganizationId);
