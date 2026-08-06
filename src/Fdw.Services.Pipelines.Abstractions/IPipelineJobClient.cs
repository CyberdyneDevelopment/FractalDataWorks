using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Defines the contract for interacting with the pipeline job service.
/// </summary>
public interface IPipelineJobClient
{
    /// <summary>
    /// Triggers a pipeline job execution.
    /// </summary>
    Task<IGenericResult<TriggerPipelineResponse>> Trigger(TriggerPipelineRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a pipeline job by execution ID.
    /// </summary>
    Task<IGenericResult<TriggerPipelineResponse>> GetStatus(Guid executionId, CancellationToken cancellationToken = default);
}
