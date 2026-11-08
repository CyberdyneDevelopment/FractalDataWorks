using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Projects.Abstractions;

/// <summary>
/// Which projects run each pipeline (FDW-4449).
/// </summary>
/// <remarks>
/// A pipeline is run by a step of a project's orchestration tree; one pipeline can sit in the steps of several projects, or of none.
/// </remarks>
public interface IPipelineProjectMemberships
{
    /// <summary>
    /// Reads, for every pipeline that a step of a project the caller may see runs, the names of those projects.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The pipeline's logical id to the sorted names of its projects. A pipeline no visible project runs is absent. The read costs a fixed number of
    /// statements however many projects there are.
    /// </returns>
    Task<IGenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<string>>>> ByPipeline(CancellationToken cancellationToken = default);
}
