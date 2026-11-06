namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// What is needed to make a tenant (FDW-1578): the writer mints its id. The organization made with it is named after the tenant
/// (FDW-4225, Mike: "the tenant and organization workspaces take the name of the entity creating them"), so the founder names only its slug.
/// </summary>
/// <param name="Name">The tenant's display name.</param>
/// <param name="Slug">The tenant's slug, unique among tenants.</param>
/// <param name="OrgPrefix">The prefix applied to permission policy names for the tenant; <see langword="null"/> for none.</param>
/// <param name="ConnectionName">The connection that isolates the tenant's data; <see langword="null"/> for none.</param>
/// <param name="IsSelectable">Whether a user can list or switch into the tenant; the founder states it (FDW-3823).</param>
/// <param name="OrganizationSlug">The slug of the organization made with the tenant, unique among organizations.</param>
public sealed record NewTenant(string Name, string Slug, string? OrgPrefix, string? ConnectionName, bool IsSelectable, string OrganizationSlug);
