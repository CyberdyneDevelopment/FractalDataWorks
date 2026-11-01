namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// What is needed to make a tenant (FDW-1578): the writer mints its id.
/// </summary>
/// <param name="Name">The tenant's display name.</param>
/// <param name="Slug">The tenant's slug, unique among tenants.</param>
/// <param name="OrgPrefix">The prefix applied to permission policy names for the tenant; <see langword="null"/> for none.</param>
/// <param name="ConnectionName">The connection that isolates the tenant's data; <see langword="null"/> for none.</param>
public sealed record NewTenant(string Name, string Slug, string? OrgPrefix, string? ConnectionName);
