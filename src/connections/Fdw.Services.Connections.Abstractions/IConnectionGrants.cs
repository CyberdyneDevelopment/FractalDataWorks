using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The connections a platform administrator grants to tenants (FDW-1581).
/// </summary>
/// <remarks>
/// Who may grant is the policy and the platform-administrator check on the endpoint that calls this; this applies the rules that do not
/// depend on the caller: the tenant and the document connection exist, the folder is a valid folder, a tenant holds one grant per
/// connection and purpose, and a grant that organizations are assigned to is kept.
/// </remarks>
public interface IConnectionGrants
{
    /// <summary>Grants a tenant a document connection for a purpose.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="connectionName">The document connection.</param>
    /// <param name="purpose">What it is granted for.</param>
    /// <param name="folder">The folder the grant is narrowed to, or <see langword="null"/> for the whole connection.</param>
    /// <param name="mayRegisterOwn">Whether the tenant may register its own connections for the purpose.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The grant, or the reason it was not made.</returns>
    Task<IGenericResult<ConnectionGrantView>> Grant(Guid tenantId, string connectionName, string purpose, string? folder, bool mayRegisterOwn, CancellationToken cancellationToken = default);

    /// <summary>Takes a grant back, unless an organization is assigned to it.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="connectionName">The connection.</param>
    /// <param name="purpose">The purpose.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The grant that was taken back, or the reason it was kept.</returns>
    Task<IGenericResult<ConnectionGrantView>> Revoke(Guid tenantId, string connectionName, string purpose, CancellationToken cancellationToken = default);

    /// <summary>Lists the connections granted to a tenant.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The grants, or the reason they could not be read.</returns>
    Task<IGenericResult<IReadOnlyList<ConnectionGrantView>>> List(Guid tenantId, CancellationToken cancellationToken = default);
}
