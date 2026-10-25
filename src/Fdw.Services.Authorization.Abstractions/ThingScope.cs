using System;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Where a thing sits in the scope tree: its tenant, and the organization of that tenant it belongs to, if any (FDW-2327).
/// </summary>
/// <remarks>
/// The tree is tenant, then organization, then workspace. A workspace sits in the organization it names
/// (<c>OrganizationId</c> on the workspace, FDW-2338), or directly under its tenant when it names none, so a thing in a workspace is
/// judged at that organization or tenant. There is no scope without a tenant: <see cref="TenantId"/> is required, and the check refuses
/// <see cref="Guid.Empty"/> instead of assuming a tenant.
/// </remarks>
/// <param name="TenantId">The tenant the thing is in.</param>
/// <param name="OrganizationId">The organization of that tenant the thing belongs to; null for a thing directly under its tenant.</param>
public sealed record ThingScope(Guid TenantId, Guid? OrganizationId);
