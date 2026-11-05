using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Endpoints.Executions;
using Fdw.Services.Etl.Tracking;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Answers what a run did (FDW-3381): its rows read, written and rejected, the size moved and its run time, and the same for each stage and each component.
/// </summary>
/// <remarks>
/// It reads the run's records and events and adds nothing: a figure the records do not hold is null, so a run from before the records carried it answers what it holds.
/// The size is an estimate of the memory the moved rows' values hold, not a size on disk or on the wire.
/// </remarks>
public abstract class GetExecutionTrackingEndpointBase : Endpoint<ExecutionIdRequest, RunTracking>
{
    private readonly IExecutionTracker _tracker;

    /// <summary>Initializes a new instance of the <see cref="GetExecutionTrackingEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="tracker">The execution tracker.</param>
    protected GetExecutionTrackingEndpointBase(ILogger logger, IExecutionTracker tracker)
    {
        EndpointLogger = logger;
        _tracker = tracker;
    }

    /// <summary>Gets the logger.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/executions/{Id}/tracking");
        Policies("pipelines:read");
        Summary(s =>
        {
            s.Summary = "Get what a run did";
            s.Description = "Returns the rows read, written and rejected, the size moved and the run time of a run, and the same for each stage and each component. A figure the run's records do not hold is null.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(ExecutionIdRequest req, CancellationToken ct)
    {
        var read = await RunTrackingReader.Read(_tracker, req.Id, ct).ConfigureAwait(false);
        if (read.IsFailure || read.Value is null)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(read, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(read.Value, ct).ConfigureAwait(false);
    }
}
