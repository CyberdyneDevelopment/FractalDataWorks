using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// Supplies the tenant a user belongs to.
/// </summary>
/// <remarks>
/// Separate from the binding because tenancy is not a property of the external identity — the same
/// person federated from the same provider still belongs to whichever tenant this platform says
/// they do.
/// </remarks>
public interface ITenantResolver
{
    /// <summary>Returns the tenant <paramref name="userId"/> belongs to.</summary>
    /// <param name="userId">The local user.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult<Guid>> TenantFor(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Returns where a sign-in of <paramref name="userId"/> lands: the tenant, and the department of it the person marked as their default (FDW-2938).</summary>
    /// <param name="userId">The local user.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>The steps that build the principal ask this, so a token is issued already acting in the department. The tenant is the same one <see cref="TenantFor"/> answers.</remarks>
    Task<IGenericResult<Landing>> LandingFor(Guid userId, CancellationToken cancellationToken = default);
}
