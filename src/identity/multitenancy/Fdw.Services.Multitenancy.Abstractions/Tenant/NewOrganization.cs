using System;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// What is needed to make an organization, which a company's people call a department (FDW-1758): the writer mints its id.
/// </summary>
/// <param name="TenantId">The tenant the organization belongs to.</param>
/// <param name="Name">The organization's display name.</param>
/// <param name="Slug">The organization's slug, unique among the organizations of the tenant.</param>
public sealed record NewOrganization(Guid TenantId, string Name, string Slug);
