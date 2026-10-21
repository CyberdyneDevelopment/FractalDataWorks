using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
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
    private readonly PipelineServiceConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="GetGraphEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The pipeline domain's configuration provider.</param>
    protected GetGraphEndpointBase(ILogger<GetGraphEndpointBase> logger, PipelineServiceConfigurationProvider provider) : base(logger)
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
    protected override Task<IGenericResult<GraphPayload?>> FindByIdentifier(PipelineNameRequest request, CancellationToken ct) =>
        GraphRead.Find(_provider, request.Name, ct);
}
