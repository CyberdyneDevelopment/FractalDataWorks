using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Reads a graph pipeline as the graph the API states (FDW-1396): the one shape a graph is read in.
/// </summary>
public abstract class GetGraphEndpointBase : CrudGetEndpointBase<PipelineNameRequest, GraphPayload>
{
    private readonly IPipelineConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="GetGraphEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The pipeline domain's configuration provider.</param>
    protected GetGraphEndpointBase(ILogger<GetGraphEndpointBase> logger, IPipelineConfigurationProvider provider) : base(logger)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "pipelines";

    /// <inheritdoc />
    protected override string Route => "/pipelines/{Name}/graph";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(PipelineNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<GraphPayload?>> FindByIdentifier(PipelineNameRequest request, CancellationToken ct)
    {
        var found = await GraphRead.Find(_provider, request.Name, ct).ConfigureAwait(false);
        if (found.IsFailure || found.Value is not { IsEnabled: false } payload)
        {
            return found;
        }

        // Why read again: the payload does not carry the marker the judge wrote when the failure rule disabled the pipeline (FDW-3864). A pipeline a person disabled has none.
        var configuration = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        if (configuration.IsFailure || configuration.Value is not GraphPipelineConfiguration { DisabledByFailureRuleAt: { } disabledAt, DisabledByFailureRuleRunId: { } runId })
        {
            return found;
        }

        var evidence = await Resolve<IPipelineDisableRecord>().Read(runId, ct).ConfigureAwait(false);
        if (evidence.IsFailure || evidence.Value is null)
        {
            return evidence.ToNewResult<GraphPayload?>();
        }

        payload.DisabledByFailureRule = new GraphDisabledByFailureRule
        {
            DisabledAt = disabledAt,
            ThresholdRuns = evidence.Value.ConsecutiveFailures,
            Reason = evidence.Value.Reason,
            Failures = [.. evidence.Value.Failures.Select(failure => new GraphRuleFailure { ExecutionId = failure.ExecutionId, At = failure.At, Category = failure.Category, Code = failure.Code })],
        };
        return found;
    }
}
