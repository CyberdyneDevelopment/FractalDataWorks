using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.UI.Pipelines.Clients.Models;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Pipelines.Clients;

/// <summary>
/// HTTP client implementation for the pipeline designer API.
/// </summary>
public sealed class PipelineDesignerApiClient : ApiClientBase, IPipelineDesignerClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineDesignerApiClient"/> class.
    /// </summary>
    public PipelineDesignerApiClient(HttpClient httpClient, ILogger<PipelineDesignerApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<TaskTypeInfo>>> GetTaskTypes(CancellationToken ct = default)
        => GetList<TaskTypeInfo>("pipelines/designer/task-types", ct);

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<PipelineStepTypeSummary>>> GetStepTypes(CancellationToken ct = default)
        => GetList<PipelineStepTypeSummary>("pipelines/designer/step-types", ct);
}
