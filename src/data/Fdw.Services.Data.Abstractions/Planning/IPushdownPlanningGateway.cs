using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// What a data gateway tells a planner that would push reads of several containers into one query (FDW-6969): what a container is, the store it
/// lives in and whether that store runs joins, read the way the gateway reads it for any command.
/// </summary>
/// <remarks>
/// A gateway that does not implement it gives a planner nothing to judge a store by, so the planner pushes nothing through it. The rows of a pushed
/// query are read through the gateway's own <see cref="IDataGateway.OpenRecordSource(Fdw.Commands.Data.Abstractions.IDataCommand, DataStoreTarget, CancellationToken)"/>.
/// </remarks>
public interface IPushdownPlanningGateway
{
    /// <summary>Describes a container of a data store for a planner.</summary>
    /// <param name="target">The data store, path and container.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The container's description, or why it cannot be resolved.</returns>
    Task<IGenericResult<PushdownContainer>> Describe(DataStoreTarget target, CancellationToken cancellationToken = default);
}
