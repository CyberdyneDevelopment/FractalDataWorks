using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Notifications.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// Sends a notification through a rule, its words and its payload made from the variables when the step runs (FDW-6138). It reads no row counts, so a run that moves nothing can send. A send that
/// fails fails the step.
/// </summary>
public sealed class FlowSend : IFlowStep
{
    private readonly IRuleNotifier _notifier;
    private readonly string _rule;
    private readonly string _subject;
    private readonly string _message;
    private readonly string? _payloadType;
    private readonly IReadOnlyList<FlowStepParameter> _fields;
    private readonly FlowNotifyRun _run;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowSend"/> class.</summary>
    /// <param name="name">The step's name.</param>
    /// <param name="notifier">Sends through a rule.</param>
    /// <param name="rule">The rule.</param>
    /// <param name="subject">The subject, which may name variables.</param>
    /// <param name="message">The message, which may name variables.</param>
    /// <param name="payloadType">The payload type, or <see langword="null"/> for a notification of words only.</param>
    /// <param name="fields">The payload's fields; empty without a payload type.</param>
    /// <param name="run">The run's facts, which say where the notification comes from.</param>
    /// <param name="logger">The logger.</param>
    public FlowSend(
        string name, IRuleNotifier notifier, string rule, string subject, string message, string? payloadType, IReadOnlyList<FlowStepParameter> fields, FlowNotifyRun run, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(rule);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Name = name;
        _notifier = notifier;
        _rule = rule;
        _subject = subject;
        _message = message;
        _payloadType = payloadType;
        _fields = fields;
        _run = run;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public async Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
    {
        string subject;
        string message;
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            subject = Graphs.Building.VariableText.Render(_subject, context.Variables);
            message = Graphs.Building.VariableText.Render(_message, context.Variables);
            foreach (var field in _fields)
            {
                fields[field.Name] = field.Value(context.Variables);
            }
        }
        catch (InvalidOperationException ex)
        {
            return GenericResult.Failure(FlowLog.SendTextNotRendered(_logger, Name, ex.Message));
        }

        var sent = await _notifier.Notify(
            new RuleNotification
            {
                RuleName = _rule,
                Subject = subject,
                Message = message,
                PipelineId = _run.PipelineId,
                Metadata = new Dictionary<string, object?>(StringComparer.Ordinal) { ["Stage"] = Name, ["Pipeline"] = _run.PipelineName },
                CorrelationId = _run.ExecutionId.ToString(),
                Payload = _payloadType is null ? null : new NotificationPayload(_payloadType, fields),
            },
            cancellationToken).ConfigureAwait(false);

        return sent.IsFailure ? GenericResult.Failure(FlowLog.NotificationSendFailed(_logger, Name, _rule, sent.CurrentMessage)) : GenericResult.Success();
    }
}
