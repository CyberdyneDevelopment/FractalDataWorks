using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Projects.Abstractions;

/// <summary>
/// Reads and assembles the rollup execution status tree for a project execution.
/// Walks IExecutionTracker.GetChildren recursively to build the full hierarchy.
/// </summary>
public interface IProjectExecutionStatusReader
{
    /// <summary>
    /// Builds the full hierarchical status tree for the given project execution item.
    /// </summary>
    /// <param name="projectExecutionItemId">
    /// The execution item identifier of the root Project execution (created by the orchestrator).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The root node of the status tree, or Failure if the execution item is not found.
    /// </returns>
    Task<IGenericResult<ProjectExecutionStatusNode>> GetStatusTree(
        Guid projectExecutionItemId,
        CancellationToken cancellationToken = default);
}
