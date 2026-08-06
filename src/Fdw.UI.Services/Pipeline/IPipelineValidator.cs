using System;
using Fdw.Results;
using Fdw.UI.Pipelines.Clients.Models;

namespace Fdw.UI.Services.Pipeline;

/// <summary>
/// Validates pipeline definitions.
/// </summary>
public interface IPipelineValidator
{
    /// <summary>
    /// Validates a pipeline definition.
    /// </summary>
    IGenericResult ValidatePipeline(PipelineEditModel pipeline);

    /// <summary>
    /// Checks if adding a connection would create a cycle.
    /// </summary>
    bool WouldCreateCycle(PipelineEditModel pipeline, Guid sourceId, Guid targetId);
}
