using System;
using System.Threading;
using Fdw.Services.Etl.Abstractions.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The time limit of one pipeline run (FDW-3805): when the request states a timeout, a timer starts as the run starts running and stops the run when the time has passed.
/// The run then ends the way a cancelled run ends, with the result code TimedOut. Disposing it disarms the timer.
/// </summary>
/// <remarks>
/// Why the time counts from the moment the run starts running and not from when it was accepted: the run waits in the queue for its place, and that wait is not the pipeline's
/// time. A pause counts: the limit is a wall-clock time. A request that states no timeout arms nothing, and a scheduled run says in the log that it has no limit.
/// </remarks>
internal sealed class PipelineRunTimeLimit : IDisposable
{
    private readonly CancellationTokenSource? _timer;
    private readonly CancellationTokenRegistration _registration;

    private PipelineRunTimeLimit(CancellationTokenSource? timer, CancellationTokenRegistration registration)
    {
        _timer = timer;
        _registration = registration;
    }

    /// <summary>Starts the run's time limit.</summary>
    /// <param name="run">The run to stop when the time has passed.</param>
    /// <param name="request">The request the run was accepted with; its timeout is the limit.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The limit; dispose it when the run ends.</returns>
    public static PipelineRunTimeLimit Arm(PipelineRunHandle run, PipelineExecutionRequest request, ILogger? logger = null) =>
        Arm(run, request, request.TimeoutSeconds is { } seconds ? TimeSpan.FromSeconds(seconds) : null, logger ?? NullLogger.Instance);

    /// <summary>Starts the run's time limit for a stated time.</summary>
    /// <param name="run">The run to stop when the time has passed.</param>
    /// <param name="request">The request the run was accepted with, for the log.</param>
    /// <param name="limit">The time; none when the request states none.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The limit; dispose it when the run ends.</returns>
    internal static PipelineRunTimeLimit Arm(PipelineRunHandle run, PipelineExecutionRequest request, TimeSpan? limit, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(request);
        if (limit is not { } time)
        {
            if (request.ScheduleName is { } schedule)
            {
                PipelineRunControlLog.RunHasNoTimeLimit(logger, schedule, request.ExecutionId, request.PipelineName);
            }

            return new PipelineRunTimeLimit(null, default);
        }

        var timer = new CancellationTokenSource(time);
        var registration = timer.Token.Register(() =>
        {
            PipelineRunControlLog.RunTimedOut(logger, request.ExecutionId, request.PipelineName, (int)time.TotalSeconds, StatedBy(request));
            run.TimeOut();
        });
        return new PipelineRunTimeLimit(timer, registration);
    }

    /// <summary>Names who stated the timeout, for the log and the run's message.</summary>
    /// <param name="request">The request.</param>
    /// <returns>The schedule, or the trigger that carried the timeout.</returns>
    internal static string StatedBy(PipelineExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.ScheduleName is { } schedule ? $"schedule '{schedule}'" : $"the {request.TriggerSource} trigger";
    }

    /// <summary>Words why a run ended as timed out.</summary>
    /// <param name="request">The request the run was accepted with.</param>
    /// <returns>The sentence the run's records state.</returns>
    internal static string Reason(PipelineExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return $"The run exceeded the {request.TimeoutSeconds} second timeout of {StatedBy(request)}";
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _registration.Dispose();
        _timer?.Dispose();
    }
}
