using Fdw.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.AnswerRequests;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Notifications.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// Judges a call under an answer policy and, when the call needs the person, keeps an answer request for the person the run acts as and tells them (FDW-6142). It is a decision: <c>go</c> fires when the call may
/// go on (the policy lets it, or a stored grant covers it), and <c>asked</c> fires when a request was kept; a call the policy refuses fails the step.
/// </summary>
/// <remarks>
/// <para>
/// Why the step is not wrapped by the claim of a step with an outside effect: the wrapper is not a decision, so it would hide the two outputs. The request is its own claim: the answer request is unique
/// for the step of the run's chain (the first run, the component and the iteration), so a restart that reaches the step again gets the request it kept, and tells the person only when they were not told.
/// </para>
/// <para>
/// A request is kept for the person the run acts as and no other; a run that acts as no person cannot ask, and no person is assumed.
/// </para>
/// </remarks>
public sealed class FlowAskStep : IFlowDecision
{
    // The name of the Card payload's field that lists what the person can do.
    private const string ActionsField = "actions";

    private readonly AskStepSettings _settings;
    private readonly FlowAskServices _services;
    private readonly IRuleNotifier _notifier;
    private readonly FlowNotifyRun _run;
    private readonly ILogger _logger;
    private int _iteration = -1;

    /// <summary>Initializes a new instance of the <see cref="FlowAskStep"/> class.</summary>
    /// <param name="name">The step's name.</param>
    /// <param name="settings">What the component states.</param>
    /// <param name="services">The answer requests, the policies and the pipelines the step uses.</param>
    /// <param name="notifier">Tells the person through the rule.</param>
    /// <param name="run">The run's facts, which say where the notification comes from.</param>
    /// <param name="logger">The logger.</param>
    public FlowAskStep(string name, AskStepSettings settings, FlowAskServices services, IRuleNotifier notifier, FlowNotifyRun run, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        _settings = settings;
        _services = services;
        _notifier = notifier;
        _run = run;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs { get; } = [AskComponentType.Go, AskComponentType.Asked];

    private sealed record Words(string Call, string Title, string Body, IReadOnlyList<string> Options, IReadOnlyList<KeyValuePair<string, string>> Arguments);

    /// <inheritdoc />
    public async Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
    {
        var iteration = Interlocked.Increment(ref _iteration);
        if (context.Identity is not { PersonId: { } person } identity)
            return Fire(context, false, false, GenericResult.Failure(AnswerRequestLog.RunHasNoPerson(_logger, Name)));

        var words = WordsOf(context.Variables);
        if (words.IsFailure || words.Value is not { } made)
            return Fire(context, false, false, words);

        var grant = GrantOf(context.Variables);
        if (grant.IsFailure)
            return Fire(context, false, false, grant);

        var judged = await _services.Policies.Judge(_settings.PolicyName, made.Call, grant.Value, cancellationToken).ConfigureAwait(false);
        if (judged.IsFailure || judged.Value is not { } judgment)
            return Fire(context, false, false, judged);

        if (judgment.Outcome.RefusesTheCall)
            return Fire(context, false, false, GenericResult.Failure(AnswerRequestLog.CallRefused(_logger, Name, judgment.PolicyName, judgment.CallName)));

        if (!judgment.Outcome.KeepsARequest)
            return Fire(context, true, false, GenericResult.Success());

        var asked = await Ask(identity, person, iteration, made, judgment, grant.Value, cancellationToken).ConfigureAwait(false);
        return Fire(context, false, asked.IsSuccess, asked);
    }

    // Records what the step fired, for the components its lines lead to, and gives back the result.
    private IGenericResult Fire(FlowStepContext context, bool go, bool asked, IGenericResult result)
    {
        FlowFired.Set(context.Variables, Name, AskComponentType.Go, go);
        FlowFired.Set(context.Variables, Name, AskComponentType.Asked, asked);
        return result;
    }

    private IGenericResult<Words> WordsOf(FlowVariables variables)
    {
        try
        {
            return GenericResult<Words>.Success(new Words(
                VariableText.Render(_settings.CallTemplate, variables),
                VariableText.Render(_settings.TitleTemplate, variables),
                VariableText.Render(_settings.BodyTemplate, variables),
                [.. _settings.OptionTemplates.Select(option => VariableText.Render(option, variables))],
                [.. _settings.Arguments.Select(argument => new KeyValuePair<string, string>(argument.Name, argument.Value(variables)))]));
        }
        catch (InvalidOperationException ex)
        {
            return GenericResult<Words>.Failure(AnswerRequestLog.TextNotRendered(_logger, Name, ex.Message));
        }
    }

    // The stored grant a grant-or-person call is judged against: the one the variable the component states holds. A variable that holds none is a refusal, not a call with no grant.
    private IGenericResult<Guid?> GrantOf(FlowVariables variables)
    {
        if (_settings.GrantVariableName is not { } variable)
            return GenericResult<Guid?>.Success(null);

        var text = variables.TryGet(variable, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty : string.Empty;
        return Guid.TryParse(text, out var grant) && grant != Guid.Empty
            ? GenericResult<Guid?>.Success(grant)
            : GenericResult<Guid?>.Failure(AnswerRequestLog.GrantVariableNotAGrant(_logger, Name, variable, text));
    }

    // Keeps the request and, when the person was not told, tells them. A request whose announcement failed stays open and listed to the person, and the step fails so the run says so.
    private async Task<IGenericResult> Ask(
        FlowRunIdentity identity, Guid person, int iteration, Words words, AnswerPolicyJudgment judgment, Guid? grant, CancellationToken cancellationToken)
    {
        var continuation = await _services.Pipelines.Get(_settings.ContinuationName, ConfigurationLevels.WholeAggregate, ConfigurationReadOptions.WithoutFilledMembers, cancellationToken).ConfigureAwait(false);
        if (continuation.IsFailure || continuation.Value is not { } target)
            return GenericResult.Failure(AnswerRequestLog.ContinuationNotFound(_logger, Name, _settings.ContinuationName, continuation.CurrentMessage));

        if (string.Equals(target.Name, _run.PipelineName, StringComparison.OrdinalIgnoreCase))
            return GenericResult.Failure(AnswerRequestLog.ContinuationIsTheAskingPipeline(_logger, Name, target.Name));

        var verified = PipelineRunParameterValues.VerifyNames(
            (target as IDeclaresRunParameters)?.DeclaredRunParameters, [.. words.Arguments.Select(argument => argument.Key), _settings.AnswerParameterName], _logger);
        if (verified.IsFailure)
            return verified;

        var kept = await _services.Requests.Ask(new AnswerRequestAsk
        {
            TenantId = identity.TenantId,
            PersonId = person,
            ExecutionId = identity.ExecutionId,
            ClaimRootId = identity.ClaimRootId,
            ComponentName = Name,
            Iteration = iteration,
            KindName = _settings.KindName,
            PolicyName = judgment.PolicyName,
            CallName = judgment.CallName,
            ExpiresAfterSeconds = judgment.ExpiresAfterSeconds,
            ContinuationPipelineId = target.Id,
            AnswerParameterName = _settings.AnswerParameterName,
            GrantId = grant,
            Shown = [new KeyValuePair<string, string>(AnswerRequestShown.Title, words.Title), new KeyValuePair<string, string>(AnswerRequestShown.Body, words.Body), .. words.Options.Select(option => new KeyValuePair<string, string>(AnswerRequestShown.Option, option))],
            Arguments = words.Arguments,
            RunRoles = identity.RunRoles
        }, cancellationToken).ConfigureAwait(false);
        if (kept.IsFailure || kept.Value is not { } made)
            return kept;

        return made.Request.AnnouncedAt is null
            ? await Announce(identity, person, made.Request, words, cancellationToken).ConfigureAwait(false)
            : GenericResult.Success();
    }

    private async Task<IGenericResult> Announce(FlowRunIdentity identity, Guid person, AnswerRequest request, Words words, CancellationToken cancellationToken)
    {
        Dictionary<string, string> fields = new(StringComparer.Ordinal) { [AnswerRequestShown.Title] = words.Title, [AnswerRequestShown.Body] = words.Body };
        if (words.Options.Count > 0)
            fields[ActionsField] = string.Join(AskComponentType.OptionSeparator.ToString(), words.Options);

        var sent = await _notifier.Notify(
            new RuleNotification
            {
                RuleName = _settings.RuleName,
                Subject = words.Title,
                Message = words.Body,
                PipelineId = _run.PipelineId,
                Metadata = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["Stage"] = Name,
                    ["Pipeline"] = _run.PipelineName,
                    ["AnswerRequestId"] = request.Id,
                    ["PersonId"] = person
                },
                CorrelationId = _run.ExecutionId.ToString(),
                Payload = new NotificationPayload(NotificationPayloadTypes.ByName("Card").Name, fields),
            },
            cancellationToken).ConfigureAwait(false);
        return sent.IsFailure
            ? GenericResult.Failure(AnswerRequestLog.AnnouncementFailed(_logger, request.Id, _settings.RuleName, sent.CurrentMessage))
            : await _services.Requests.Announced(identity.TenantId, request.Id, cancellationToken).ConfigureAwait(false);
    }
}
