using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.ServiceTypes;

namespace Fdw.Services.DataVault.Abstractions;

/// <summary>
/// Provider for configured data vault instances.
/// </summary>
public interface IDataVaultProvider : IPlatformServiceProvider<IDataVault, IDataVaultImplementationConfiguration>
{
    /// <summary>
    /// Gets a vault matching the supplied typed request (Id and/or Name).
    /// An empty request (neither Id nor Name) is a structured failure.
    /// </summary>
    /// <param name="request">The typed vault lookup request.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult<IDataVault>> Get(DataVaultRequest request, CancellationToken cancellationToken = default);
}
