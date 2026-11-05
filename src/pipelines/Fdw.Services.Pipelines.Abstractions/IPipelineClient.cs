using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Defines the contract for interacting with the pipeline configuration API.
/// </summary>
public interface IPipelineClient
{
    /// <summary>
    /// Lists all configured pipelines.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<PipelineSummaryResponse>>> List(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the configured pipelines that carry every one of the named tags (FDW-3816).
    /// </summary>
    /// <param name="tags">The tag names, sent as <c>?tag=</c>; none lists every pipeline.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult<IReadOnlyList<PipelineSummaryResponse>>> List(IReadOnlyList<string> tags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a pipeline by name.
    /// </summary>
    Task<IGenericResult<PipelineDetailResponse>> Get(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a pipeline by its logical id, as the caller sees it (FDW-1088).
    /// </summary>
    Task<IGenericResult<PipelineDetailResponse>> Get(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a graph pipeline as the graph the API states (FDW-1396).
    /// </summary>
    Task<IGenericResult<GraphPayload>> GetGraph(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a graph pipeline (FDW-1396); the graph is refused whole when it cannot run.
    /// </summary>
    Task<IGenericResult<GraphPayload>> CreateGraph(GraphPayload graph, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes a graph pipeline from a template and the values given for the template's parameters (FDW-3379).
    /// </summary>
    Task<IGenericResult<GraphPayload>> CreateGraphFromTemplate(GraphFromTemplatePayload request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the pipelines that are templates (FDW-3379).
    /// </summary>
    Task<IGenericResult<IReadOnlyList<PipelineSummaryResponse>>> ListTemplates(CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces a graph pipeline whole with the graph in the request (FDW-1396).
    /// </summary>
    Task<IGenericResult<GraphPayload>> ReplaceGraph(string name, GraphPayload graph, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks a graph draft without saving it and answers every problem with the path to the part it concerns (FDW-1396).
    /// </summary>
    Task<IGenericResult<GraphValidationPayload>> ValidateGraph(GraphPayload graph, CancellationToken cancellationToken = default);

    /// <summary>
    /// Describes a graph draft: the fields a row has at every input and output node, and the problems the draft has (FDW-1397).
    /// </summary>
    Task<IGenericResult<GraphDescriptionPayload>> DescribeGraph(GraphPayload graph, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a pipeline by name.
    /// </summary>
    Task<IGenericResult> DeletePipeline(string name, CancellationToken cancellationToken = default);
}
