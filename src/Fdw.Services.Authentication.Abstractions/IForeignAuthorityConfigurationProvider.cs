using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Reads the foreign authority a flow's exchange step trusts.
/// </summary>
/// <remarks>
/// The step takes this rather than a configuration object, so the authority is read where every
/// other configuration is read and a host changing which authority it trusts changes a row rather
/// than a deployment.
/// </remarks>
public interface IForeignAuthorityConfigurationProvider
{
    /// <summary>Reads the authority this deployment declared.</summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The authority, or a failure naming what was missing.</returns>
    /// <remarks>
    /// No name, because a step does not know one: the resolver maps a flow's step name to a type
    /// and the type never learns what it was called. An implementation reading more than one
    /// declared authority therefore has nothing to choose between them with, and should refuse
    /// rather than pick by order.
    /// </remarks>
    Task<IGenericResult<IForeignAuthorityConfiguration>> Get(
        CancellationToken cancellationToken = default);
}
