using System;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// What is needed to make an organization, which a company's people call a department (FDW-1758): the writer mints its id.
/// </summary>
/// <param name="TenantId">The tenant the organization belongs to.</param>
/// <param name="Name">The organization's display name.</param>
/// <param name="Slug">The organization's slug, unique among the organizations of the tenant.</param>
/// <param name="IsDefault">Whether this is the tenant's default organization (FDW-3668). Only a tenant's founding states it; the tenant has at most one.</param>
/// <param name="DefaultWorkspaceId">The durable id of the organization's own default workspace, which the founding states when it makes the workspace (FDW-4600); <see langword="null"/> when the setting <c>FoundingMakesDefaultWorkspace</c> is off and the organization has none.</param>
public sealed record NewOrganization(Guid TenantId, string Name, string Slug, bool IsDefault = false, Guid? DefaultWorkspaceId = null);
