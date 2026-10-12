using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Configuration;

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
}
