using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Configuration;

namespace Fdw.Services.Connections;

/// <summary>Supplies the ConnectionAssignment configuration (FDW-1581).</summary>
public interface IConnectionAssignmentConfigurationProvider
    : IDomainConfigurationProvider<IConnectionAssignmentImplementationConfiguration>
{
    /// <summary>
    /// Marks the assignment in force superseded and saves the one that replaces it, together: both land or neither does (FDW-3396).
    /// </summary>
    /// <param name="superseded">The assignment in force, with its <see cref="IConnectionAssignmentImplementationConfiguration.SupersededAt"/> set.</param>
    /// <param name="replacement">The new assignment, under a name of its own.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success once both are saved, or the failure of the write that did not land.</returns>
    Task<IGenericResult> Supersede(
        ConnectionAssignmentImplementationConfiguration superseded,
        ConnectionAssignmentImplementationConfiguration replacement,
        CancellationToken cancellationToken = default);
}
