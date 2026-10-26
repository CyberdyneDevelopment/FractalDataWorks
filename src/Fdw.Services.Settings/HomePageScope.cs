using System;

namespace Fdw.Services.Settings;

/// <summary>
/// The scope that holds a home page: a kind, the id of the organization, tenant or person, and the tenant it is in (FDW-2325).
/// </summary>
/// <param name="Kind">One of <see cref="HomePageKinds"/>: <c>Person</c>, <c>Organization</c> or <c>Tenant</c>.</param>
/// <param name="Id">The id of the person, organization or tenant.</param>
/// <param name="TenantId">The tenant the scope is in; a tenant's own scope names itself.</param>
public sealed record HomePageScope(string Kind, Guid Id, Guid TenantId);
