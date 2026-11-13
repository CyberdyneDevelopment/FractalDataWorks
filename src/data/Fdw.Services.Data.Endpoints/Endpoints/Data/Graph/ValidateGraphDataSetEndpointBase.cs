using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Checks a Graph data set draft without saving it (FDW-5290) and answers every problem, each with its result code and the path of the field out, component or
/// link it concerns, and the intended sources that are not bound yet.
/// </summary>
/// <remarks>
/// It takes the body of <c>POST /datasets/graph</c> and judges what saving it would judge, and what the engine's check of a graph pipeline would judge of its
/// components and links. It saves nothing and judges no name. An empty list of problems means saving would be accepted; an intended source that is not bound yet
/// is reported apart and never makes the draft invalid.
/// </remarks>
public abstract class ValidateGraphDataSetEndpointBase : Endpoint<CreateGraphDataSetRequest, GraphDataSetValidationPayload>
{
    private readonly ILogger<ValidateGraphDataSetEndpointBase> _logger;
    private readonly GraphDataSetSources _sources;

    /// <summary>Initializes a new instance of the <see cref="ValidateGraphDataSetEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSets">The data set domain's configuration provider.</param>
    /// <param name="dataStores">The data store domain's configuration provider.</param>
    protected ValidateGraphDataSetEndpointBase(
        ILogger<ValidateGraphDataSetEndpointBase> logger, IDataSetConfigurationProvider dataSets, IDataStoreConfigurationProvider dataStores)
    {
        _logger = logger;
        _sources = new GraphDataSetSources(dataSets, dataStores);
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/datasets/graph/validate");
        DontThrowIfValidationFails();
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datasets:write");
#endif
        Summary(s =>
        {
            s.Summary = "Check a Graph data set draft without saving it";
            s.Description = "Takes the body of POST /datasets/graph and answers every problem saving it would be refused for, each with its result code and the path of the field out, component or link it concerns, and the intended sources that are not bound yet. An empty list of problems means saving would accept the draft.";
        });
        Tags("DataSets");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CreateGraphDataSetRequest req, CancellationToken ct)
    {
        var answer = await GraphDataSetValidator.Validate(req, _sources, _logger, ct).ConfigureAwait(false);
        if (answer.IsFailure || answer.Value is null)
        {
            var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(answer, HttpContext);
            await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(answer.Value, ct).ConfigureAwait(false);
    }
}
