using Fdw.Services.Multitenancy.Abstractions;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Maps an organization to its summary response (FDW-1758).
/// </summary>
internal static class OrganizationMap
{
    internal static OrganizationSummaryResponse Of(OrganizationConfiguration organization)
        => new()
        {
            Id = organization.Id,
            Name = organization.Name,
            Slug = organization.Slug,
            IsDefault = organization.IsDefault,
            IsAdminOrg = organization.IsAdminOrg,
            IsActive = organization.IsActive,
            DefaultWorkspaceId = organization.DefaultWorkspaceId,
        };
}
