using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// Starts another pipeline from the control flow (FDW-1403): StartPipeline as a step, with its parameters made from variables and
/// its outcome captured into a variable.
/// </summary>
/// <remarks>
/// <para>
/// The started pipeline is a child run of the run this step belongs to, in its tenant and with its roles (the starter and the
/// identity are the run's own). With <c>awaitCompletion</c> the step waits for the child to end and may set a variable to whether
/// it succeeded and fail when it did not; without it the step returns once the child is queued.
/// </para>
/// <para>
/// A run that is untracked or a host that cannot start pipelines fails the step; the step never starts the pipeline unlinked.
/// </para>
/// <para>
/// With a run point (FDW-2043) the step is told when to start the pipeline, as <see cref="FlowNotify"/> is: after a stage or after the load it starts it
/// when the run reaches the step; on failure it does nothing when reached and starts it, through <see cref="IFlowFailureHook"/>, only if the run
/// fails. A step with a run point can name the run's facts (pipeline name, execution id, start and end time, outcome, rows extracted, rejected and
/// loaded) in its parameter values.
/// </para>
/// </remarks>
public sealed class FlowExecutePipeline : IFlowStep, IFlowFailureHook
{
    private readonly string _pipeline;
    private readonly IReadOnlyList<FlowStepParameter> _parameters;
    private readonly bool _awaitCompletion;
    private readonly string? _resultVariable;
    private readonly bool _failOnChildFailure;
    private readonly IRunPoint? _runPoint;
    private readonly FlowNotifyRun? _run;
    private readonly Func<long>? _loaded;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowExecutePipeline"/> class.</summary>
    /// <param name="name">The step's name.</param>
    /// <param name="pipeline">The name of the pipeline to start.</param>
    /// <param name="parameters">The values of its parameters, made from the variables when the step runs.</param>
    /// <param name="awaitCompletion">Whether the step waits for the started pipeline to end.</param>
    /// <param name="resultVariable">A declared variable set to whether the started pipeline succeeded; null for none. Requires <paramref name="awaitCompletion"/>.</param>
    /// <param name="failOnChildFailure">Whether the step fails when the started pipeline does not succeed. Requires <paramref name="awaitCompletion"/>.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="runPoint">When the step starts the pipeline; null starts it when the run reaches the step, with no run facts for the parameters.</param>
    /// <param name="run">What the run is, for the facts a parameter value may name; required with a run point.</param>
    /// <param name="loaded">How many rows the run's destinations have loaded so far; required with a run point.</param>
    public FlowExecutePipeline(
        string name,
        string pipeline,
        IReadOnlyList<FlowStepParameter> parameters,
        bool awaitCompletion,
        string? resultVariable,
        bool failOnChildFailure,
        ILogger? logger = null,
        IRunPoint? runPoint = null,
        FlowNotifyRun? run = null,
        Func<long>? loaded = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeline);
        ArgumentNullException.ThrowIfNull(parameters);
        if (!awaitCompletion && (resultVariable is not null || failOnChildFailure))
        {
            throw new ArgumentException("A result variable or a failure on the child's failure needs the step to wait for the child.", nameof(awaitCompletion));
        }

        Name = name;
        _pipeline = pipeline;
        _parameters = parameters;
        _awaitCompletion = awaitCompletion;
        _resultVariable = resultVariable;
        _failOnChildFailure = failOnChildFailure;
        if (runPoint is not null && (run is null || loaded is null))
        {
            throw new ArgumentException("A run point needs the run and its loaded rows, so the parameters can name the run's facts.", nameof(runPoint));
        }

        _runPoint = runPoint;
        _run = run;
        _loaded = loaded;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _runPoint is { FiresOnFailure: true }
            ? Task.FromResult<IGenericResult>(GenericResult.Success())
            : Start(context, "Succeeded", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> OnFailure(FlowStepContext context, IGenericResult failure, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(failure);
        return _runPoint is { FiresOnFailure: true }
            ? await Start(context, "Failed", cancellationToken).ConfigureAwait(false)
            : GenericResult.Success();
    }

    private async Task<IGenericResult> Start(FlowStepContext context, string outcome, CancellationToken cancellationToken)
    {
        if (context.Starter is null)
        {
            return GenericResult.Failure(FlowLog.ExecuteUnavailable(_logger, Name, _pipeline, "run starter"));
        }

        if (context.Identity is null)
        {
            return GenericResult.Failure(FlowLog.ExecuteUnavailable(_logger, Name, _pipeline, "tracked execution"));
        }

        if (_resultVariable is not null)
        {
            var target = FlowVariableTarget.Check(context.Variables, Name, _resultVariable, _logger);
            if (target.IsFailure)
            {
                return target;
            }
        }

        var scope = ParameterScope(context, outcome);
        if (scope.IsFailure || scope.Value is null)
        {
            return scope;
        }

        var started = await context.Starter.Start(StartRequest(context, scope.Value), cancellationToken).ConfigureAwait(false);
        if (started.IsFailure)
        {
            return started;
        }

        return _awaitCompletion
            ? await Finish(context, started.Value, cancellationToken).ConfigureAwait(false)
            : GenericResult.Success();
    }

    // The variables the parameter values are made from: the step's own, and with a run point the run's facts as they are now (FDW-2043).
    private IGenericResult<FlowVariables> ParameterScope(FlowStepContext context, string outcome)
    {
        if (_run is null || _loaded is null)
        {
            return GenericResult<FlowVariables>.Success(context.Variables);
        }

        if (context.Executor is not IFlowRunCounts counts)
        {
            return GenericResult<FlowVariables>.Failure(FlowLog.ExecuteUnavailable(_logger, Name, _pipeline, "count of the rows of the run"));
        }

        var facts = new NotifyRunFacts(
            _run.PipelineId, _run.PipelineName, _run.ExecutionId, Saturate(counts.Extracted), Saturate(counts.Rejected), Saturate(_loaded()),
            _run.StartedAt, DateTimeOffset.UtcNow, outcome);
        var scope = context.Variables.CreateChild();
        foreach (var variable in NotifyVariables.All())
        {
            scope.Declare(variable.Name, variable.ValueOf(facts));
        }

        return GenericResult<FlowVariables>.Success(scope);
    }

    private static int Saturate(long value) => value > int.MaxValue ? int.MaxValue : (int)value;

    private PipelineRunStart StartRequest(FlowStepContext context, FlowVariables variables) => new()
    {
        ParentExecutionId = context.Identity!.ExecutionId,
        PipelineName = _pipeline,
        TenantId = context.Identity.TenantId,
        RunRoles = context.Identity.RunRoles,
        TriggerSource = $"Step:{Name}",
        Parameters = [.. _parameters.Select(p => new KeyValuePair<string, string>(p.Name, p.Value(variables)))],
        AwaitCompletion = _awaitCompletion
    };

    private async Task<IGenericResult> Finish(FlowStepContext context, Guid executionId, CancellationToken cancellationToken)
    {
        var succeeded = await context.Starter!.Await(executionId, cancellationToken).ConfigureAwait(false);
        if (_resultVariable is not null)
        {
            context.Variables.TrySet(_resultVariable, succeeded);
        }

        return !succeeded && _failOnChildFailure
            ? GenericResult.Failure(FlowLog.ChildPipelineFailed(_logger, Name, _pipeline))
            : GenericResult.Success();
    }
}
