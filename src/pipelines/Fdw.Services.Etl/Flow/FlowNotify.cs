using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Notifications.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A step that sends a notification through a notification rule, with what the run has done so far in the message (FDW-1615).
/// </summary>
/// <remarks>
/// A step set to a run point other than <c>OnFailure</c> sends when the sequence reaches it. A step set to <c>OnFailure</c> does
/// nothing when reached and sends only if the run fails, through <see cref="IFlowFailureHook"/>. A notification that cannot be sent
/// fails the step, so a run never reports success while a notification it promised was lost; a notification sent for a run that has
/// already failed is logged when it cannot be sent, and the run's own failure is the answer.
/// </remarks>
public sealed class FlowNotify : IFlowStep, IFlowFailureHook
{
    private readonly IRuleNotifier _notifier;
    private readonly string _ruleName;
    private readonly string _message;
    private readonly IRunPoint _runPoint;
    private readonly Guid _pipelineId;
    private readonly string _pipelineName;
    private readonly Guid _executionId;
    private readonly DateTimeOffset _startedAt;
    private readonly Func<long> _loaded;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowNotify"/> class.</summary>
    /// <param name="name">The step's name.</param>
    /// <param name="notifier">Sends through a notification rule.</param>
    /// <param name="ruleName">The notification rule to send through.</param>
    /// <param name="runPoint">When the notification is sent.</param>
    /// <param name="message">The message template.</param>
    /// <param name="run">What the run is: its pipeline, its execution and when it started.</param>
    /// <param name="loaded">How many rows the run's destinations have loaded so far.</param>
    /// <param name="logger">The logger.</param>
    public FlowNotify(string name, IRuleNotifier notifier, string ruleName, IRunPoint runPoint, string message, FlowNotifyRun run, Func<long> loaded, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleName);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(run);
        Name = name;
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        _ruleName = ruleName;
        _runPoint = runPoint ?? throw new ArgumentNullException(nameof(runPoint));
        _message = message;
        _pipelineId = run.PipelineId;
        _pipelineName = run.PipelineName;
        _executionId = run.ExecutionId;
        _startedAt = run.StartedAt;
        _loaded = loaded ?? throw new ArgumentNullException(nameof(loaded));
        _logger = logger ?? NullLogger.Instance;
        var unknown = NotifyTemplate.Unknown(message);
        if (unknown.Count > 0)
        {
            throw new ArgumentException($"The message uses unknown variables [{string.Join(", ", unknown)}]; a run has [{NotifyTemplate.Reserved}].", nameof(message));
        }
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _runPoint.FiresOnFailure
            ? Task.FromResult<IGenericResult>(GenericResult.Success())
            : Send(context, "Succeeded", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> OnFailure(FlowStepContext context, IGenericResult failure, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(failure);
        return _runPoint.FiresOnFailure
            ? await Send(context, "Failed", cancellationToken).ConfigureAwait(false)
            : GenericResult.Success();
    }

    private async Task<IGenericResult> Send(FlowStepContext context, string outcome, CancellationToken cancellationToken)
    {
        if (context.Executor is not IFlowRunCounts counts)
        {
            return GenericResult.Failure(NotifyLog.NotifyStageInvalid(_logger, Name, "the run does not count its rows, so a message cannot say how many there were"));
        }

        var facts = new NotifyRunFacts(
            _pipelineId, _pipelineName, _executionId, Saturate(counts.Extracted), Saturate(counts.Rejected), Saturate(_loaded()),
            _startedAt, DateTimeOffset.UtcNow, outcome);
        var message = NotifyTemplate.Expand(Name, _message, facts, _logger);
        if (message.IsFailure || message.Value is null)
        {
            return message;
        }

        var sent = await _notifier.Notify(
            new RuleNotification
            {
                RuleName = _ruleName,
                Subject = $"{_pipelineName}: {Name}",
                Message = message.Value,
                PipelineId = _pipelineId,
                Metadata = Metadata(facts),
                CorrelationId = _executionId.ToString(),
            },
            cancellationToken).ConfigureAwait(false);
        if (sent.IsFailure)
        {
            NotifyLog.NotifyFailed(_logger, Name, _runPoint.Name, _ruleName, sent.CurrentMessage);
            return sent;
        }

        NotifyLog.NotifyFired(_logger, Name, _runPoint.Name, _ruleName);
        return sent;
    }

    private Dictionary<string, object?> Metadata(NotifyRunFacts facts)
    {
        var metadata = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Stage"] = Name,
            ["RunPoint"] = _runPoint.Name,
        };
        foreach (var variable in NotifyVariables.All())
        {
            metadata[variable.Name] = variable.ValueOf(facts);
        }

        return metadata;
    }

    private static int Saturate(long value) => value > int.MaxValue ? int.MaxValue : (int)value;
}
