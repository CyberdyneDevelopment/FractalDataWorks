using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Authorization;

/// <summary>
/// Reads the role-to-permission grants.
/// </summary>
public interface IRolePermissionConfigurationProvider 
    : IDomainConfigurationProvider<IRolePermissionImplementationConfiguration>
{
    /// <summary>Replaces a role's whole permission set in one operation.</summary>
    /// <param name="retiring">The ids of the mappings the role holds now, which go.</param>
    /// <param name="mappings">The mappings the role should hold instead.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success once the whole set is in place, or the failure that stopped it.</returns>
    /// <remarks>
    /// One operation rather than a delete and a save per row, because a half-applied permission set
    /// leaves a role holding neither the set it had nor the set it was given. Making the individual
    /// writes transactional does not give this: each would still commit on its own.
    /// </remarks>
    Task<IGenericResult> ReplacePermissions(
        IReadOnlyList<Guid> retiring,
        IReadOnlyList<IRolePermissionImplementationConfiguration> mappings,
        CancellationToken cancellationToken = default);

    /// <summary>Removes, in <paramref name="unit"/>, every permission grant of a role (FDW-5222).</summary>
    /// <param name="roleId">The durable Id of the role whose grants go.</param>
    /// <param name="unit">The unit the removal joins; it commits or rolls back with the rest of the unit.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>How many grants were removed; otherwise the failure of the read or of the first removal that failed.</returns>
    /// <remarks>For the delete of the role itself, so no grant names a role that no longer exists.</remarks>
    Task<IGenericResult<int>> RemoveGrantsOf(Guid roleId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default);
}
