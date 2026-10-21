using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Which features of the catalogue are on for a tenant and for its organizations (FDW-1580).
/// </summary>
/// <remarks>
/// A feature is on for a tenant when the platform has switched it on, and on for an organization when the tenant has switched it on for
/// it as well: off at a higher level means off below. Switching a feature off for a tenant leaves what the tenant set for its organizations
/// in place but ignored, so the feature returns as the tenant had it if the platform switches it on again. Who may switch what is settled
/// by the caller; the organization is checked against its tenant by the caller too, which passes both.
/// </remarks>
public interface IFeatureEnablement
{
    /// <summary>Reads every feature of the catalogue as it stands for a tenant.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The catalogue, each feature on or off with the level that has not switched it on.</returns>
    Task<IGenericResult<IReadOnlyList<FeatureState>>> ForTenant(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Reads every feature of the catalogue as it stands for an organization.</summary>
    /// <param name="tenantId">The tenant the organization belongs to.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The catalogue, each feature on or off with the level that has not switched it on.</returns>
    Task<IGenericResult<IReadOnlyList<FeatureState>>> ForOrganization(Guid tenantId, Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>Switches a feature on or off for a tenant.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="featureName">The feature, by its name in the catalogue.</param>
    /// <param name="enabled">True to switch it on, false to switch it off.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The feature as it now stands for the tenant, or a not-found failure when the catalogue has none of that name.</returns>
    Task<IGenericResult<FeatureState>> SetForTenant(Guid tenantId, string featureName, bool enabled, CancellationToken cancellationToken = default);

    /// <summary>Switches a feature on or off for an organization.</summary>
    /// <param name="tenantId">The tenant the organization belongs to.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="featureName">The feature, by its name in the catalogue.</param>
    /// <param name="enabled">True to switch it on, false to switch it off.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The feature as it now stands for the organization, or a conflict failure when switching it on and the tenant does not have it.</returns>
    Task<IGenericResult<FeatureState>> SetForOrganization(
        Guid tenantId, Guid organizationId, string featureName, bool enabled, CancellationToken cancellationToken = default);
}
