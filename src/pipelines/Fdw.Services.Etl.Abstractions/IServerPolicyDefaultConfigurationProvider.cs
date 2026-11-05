using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Abstractions;

/// <summary>
/// Reads the ServerPolicyDefault domain: the server's policy defaults, the top of the project/stage/step policy cascade.
/// </summary>
public interface IServerPolicyDefaultConfigurationProvider
    : IDomainConfigurationProvider<IServerPolicyDefaultImplementationConfiguration>
{
    /// <summary>
    /// Gets the server's policy defaults: the domain's one current row.
    /// </summary>
    /// <param name="ct">A token to cancel the read.</param>
    /// <returns>The row, or a failure when the domain cannot be read or does not hold exactly one current row.</returns>
    Task<IGenericResult<IServerPolicyDefaultImplementationConfiguration>> GetCurrent(CancellationToken ct = default);
}
