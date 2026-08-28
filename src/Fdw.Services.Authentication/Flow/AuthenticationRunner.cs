using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Execution;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Flow;

/// <summary>
/// Runs a flow's steps in order and issues a token if they establish enough to warrant one.
/// </summary>
/// <remarks>
/// Every guarantee the pipeline makes lives here, because a guarantee spread across the steps is a
/// guarantee only the well-behaved steps keep. A step package is added by configuration, and
/// configuration is not a code review — so a hostile or merely careless step must be unable to
/// elevate, however it behaves.
/// </remarks>
public sealed class AuthenticationRunner
{
    private readonly IAuthenticationStepResolver _steps;
    private readonly IAcrPolicy _acrPolicy;
    private readonly ITokenIssuer _issuer;
    private readonly IAuthenticationExecutionStore _executions;
    private readonly TimeSpan _executionLifetime;
    private readonly ILogger<AuthenticationRunner> _logger;

    /// <summary>Initializes a new instance of the <see cref="AuthenticationRunner"/> class.</summary>
    /// <param name="steps">Resolves a step by the name a flow gives.</param>
    /// <param name="acrPolicy">Turns proved methods into an assurance level.</param>
    /// <param name="issuer">Mints the token once the terminal check passes.</param>
    /// <param name="executions">Holds a suspended flow between the redirect and the return.</param>
    /// <param name="executionLifetime">How long a suspended flow stays resumable.</param>
    /// <param name="logger">The logger.</param>
    public AuthenticationRunner(
        IAuthenticationStepResolver steps,
        IAcrPolicy acrPolicy,
        ITokenIssuer issuer,
        IAuthenticationExecutionStore executions,
        TimeSpan executionLifetime,
        ILogger<AuthenticationRunner>? logger = null)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _acrPolicy = acrPolicy ?? throw new ArgumentNullException(nameof(acrPolicy));
        _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        _executions = executions ?? throw new ArgumentNullException(nameof(executions));
        _executionLifetime = executionLifetime;
        _logger = logger ?? NullLogger<AuthenticationRunner>.Instance;
    }

    /// <summary>Runs <paramref name="flow"/> from its first step.</summary>
    /// <param name="flow">The flow to run.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public Task<IGenericResult<FlowResult>> Run(
        AuthenticationFlow flow, CancellationToken cancellationToken = default)
        => flow is null
            ? Task.FromResult(GenericResult<FlowResult>.Failure(RunnerLog.FlowMissing(_logger)))
            : Execute(flow, new AuthenticationContext(), 0, cancellationToken);

    /// <summary>Resumes a suspended flow.</summary>
    /// <param name="flow">The flow the execution belongs to.</param>
    /// <param name="resumeToken">The token handed out when it suspended.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task<IGenericResult<FlowResult>> Resume(
        AuthenticationFlow flow, string resumeToken, CancellationToken cancellationToken = default)
    {
        var consumed = await _executions.TryConsume(resumeToken, cancellationToken).ConfigureAwait(false);
        if (consumed.IsFailure)
            return consumed.ToNewResult<FlowResult>();

        var record = consumed.Value!;
        if (!string.Equals(record.FlowName, flow.Name, StringComparison.Ordinal))
            return GenericResult<FlowResult>.Failure(
                RunnerLog.ExecutionFlowMismatch(_logger, record.Id, record.FlowName, flow.Name));

        RunnerLog.FlowResuming(_logger, flow.Name, record.Id, record.CurrentStepIndex);

        return await Execute(flow, record.Context, record.CurrentStepIndex, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IGenericResult<FlowResult>> Execute(
        AuthenticationFlow flow, AuthenticationContext context, int startAt, CancellationToken cancellationToken)
    {
        if (startAt == 0)
            RunnerLog.FlowStarting(_logger, flow.Name, flow.Steps.Count);

        for (var i = startAt; i < flow.Steps.Count; i++)
        {
            var resolved = _steps.Resolve(flow.Steps[i]);
            if (resolved.IsFailure)
                return resolved.ToNewResult<FlowResult>();

            var step = resolved.Value!;
            RunnerLog.StepExecuting(_logger, flow.Name, flow.Steps[i], i);

            // I3 — enforced here and not only when the configuration loaded, because a step that
            // returned NotApplicable contributed nothing however valid the declared order was.
            if (!context.Satisfies(step.Requires))
                return GenericResult<FlowResult>.Failure(RunnerLog.RequirementMissing(
                    _logger, flow.Name, flow.Steps[i],
                    string.Join(", ", step.Requires.Where(r => !context.Has(r)))));

            var outcome = await step.Execute(context, cancellationToken).ConfigureAwait(false);
            if (outcome.IsFailure)
                return outcome.ToNewResult<FlowResult>();

            ContextContribution? contributed = null;

            switch (outcome.Value)
            {
                case StepOutcome.Contributed c:
                    contributed = c.Contribution;
                    context = Merge(context, contributed, step, flow.Steps[i]);
                    RunnerLog.StepContributed(_logger, flow.Steps[i],
                        string.Join(", ", contributed.Present()));
                    break;

                case StepOutcome.Challenge challenge:
                    return await Suspend(flow, context, i, cancellationToken)
                        .ContinueWith(t => t.Result.IsFailure
                            ? t.Result.ToNewResult<FlowResult>()
                            : GenericResult<FlowResult>.Success(
                                new FlowResult.Suspended(challenge.RedirectTo, t.Result.Value!)),
                            cancellationToken, TaskContinuationOptions.ExecuteSynchronously,
                            TaskScheduler.Current).ConfigureAwait(false);

                case StepOutcome.Pending pending:
                    return await Suspend(flow, context, i, cancellationToken)
                        .ContinueWith(t => t.Result.IsFailure
                            ? t.Result.ToNewResult<FlowResult>()
                            : GenericResult<FlowResult>.Success(
                                new FlowResult.Waiting(pending.PollAfter, t.Result.Value!)),
                            cancellationToken, TaskContinuationOptions.ExecuteSynchronously,
                            TaskScheduler.Current).ConfigureAwait(false);

                case StepOutcome.NotApplicable notApplicable:
                    RunnerLog.StepNotApplicable(_logger, flow.Steps[i], notApplicable.Reason);
                    continue;

                default:
                    return GenericResult<FlowResult>.Failure(
                        RunnerLog.UnknownOutcome(_logger, flow.Steps[i]));
            }

            // I2 — the intersection, and only once the step has actually succeeded. A step that
            // reports a method it never declared records nothing: the declaration is the ceiling.
            foreach (var method in Recordable(step, contributed))
            {
                context = context with { AchievedMethods = [.. context.AchievedMethods, method] };
                RunnerLog.MethodRecorded(_logger, flow.Steps[i], method);
            }
        }

        context = context with { AchievedAcr = _acrPolicy.Evaluate(context.AchievedMethods) };
        RunnerLog.AssuranceEvaluated(_logger,
            string.Join(", ", context.AchievedMethods), context.AchievedAcr ?? "none");

        return await Terminal(flow, context, cancellationToken).ConfigureAwait(false);
    }

    // I2 — what the step observed, kept only where it also declared it may assert it. A step
    // declaring nothing proves nothing; a step declaring methods and observing none is taken to
    // have proved what it declared, which is the ordinary case for a step doing its own checking.
    private static IEnumerable<string> Recordable(IAuthenticationStep step, ContextContribution? contribution)
        => step.AuthenticationMethods.Count == 0
            ? []
            : contribution is null or { ObservedMethods.Count: 0 }
                ? step.AuthenticationMethods
                : contribution.ObservedMethods.Where(step.AuthenticationMethods.Contains);

    // I1 — a step's output is filtered to what it declared. Anything else is discarded and
    // reported: a declaration nothing checks is a comment, and silence would make this a latent
    // bug rather than an alarm.
    private AuthenticationContext Merge(
        AuthenticationContext context, ContextContribution contribution, IAuthenticationStep step, string stepName)
    {
        foreach (var element in contribution.Present().Where(e => !step.Contributes.Contains(e)))
            RunnerLog.UndeclaredContribution(_logger, stepName, element.ToString());

        var declared = step.Contributes;

        return context with
        {
            Subject = declared.Contains(ContextElement.Subject) && contribution.Subject is not null
                ? contribution.Subject : context.Subject,
            Principal = declared.Contains(ContextElement.Principal) && contribution.Principal is not null
                ? contribution.Principal : context.Principal,
            Claims = declared.Contains(ContextElement.Claims) && contribution.Claims.Count > 0
                ? context.Claims.Add(contribution.Claims) : context.Claims,
            Decision = declared.Contains(ContextElement.Decision) && contribution.Decision is not null
                ? contribution.Decision : context.Decision,
        };
    }

    private async Task<IGenericResult<string>> Suspend(
        AuthenticationFlow flow, AuthenticationContext context, int stepIndex, CancellationToken cancellationToken)
    {
        RunnerLog.FlowSuspended(_logger, flow.Name, flow.Steps[stepIndex]);

        return await _executions.Suspend(
            new ExecutionRecord
            {
                Id = Guid.NewGuid(),
                FlowName = flow.Name,
                Context = context,
                CurrentStepIndex = stepIndex,
                ExpiresAt = DateTimeOffset.UtcNow.Add(_executionLifetime),
            },
            cancellationToken).ConfigureAwait(false);
    }

    // I4 — the terminal check. Not a step, so a flow cannot be configured to omit it.
    private async Task<IGenericResult<FlowResult>> Terminal(
        AuthenticationFlow flow, AuthenticationContext context, CancellationToken cancellationToken)
    {
        if (context.Subject is null)
            return GenericResult<FlowResult>.Failure(RunnerLog.NoSubject(_logger, flow.Name));

        if (context.Principal is null)
            return GenericResult<FlowResult>.Failure(RunnerLog.NoPrincipal(_logger, flow.Name));

        if (context.Decision is not { Permitted: true })
            return GenericResult<FlowResult>.Failure(RunnerLog.NotPermitted(
                _logger, flow.Name, context.Decision?.Reason ?? "no decision was reached"));

        if (!_acrPolicy.Meets(context.AchievedAcr, flow.MinimumAcr))
            return GenericResult<FlowResult>.Failure(RunnerLog.InsufficientAssurance(
                _logger, flow.Name, context.AchievedAcr ?? "none", flow.MinimumAcr ?? "none"));

        RunnerLog.TerminalPassed(_logger, flow.Name, flow.Audience);

        var issued = await _issuer.Issue(
            new IssuanceRequest
            {
                PrincipalId = context.Principal.Id,
                TenantId = context.Principal.TenantId,
                Audience = flow.Audience,
                Scopes = [],
                AuthenticationMethods = context.AchievedMethods,
                Acr = context.AchievedAcr,

                // I6 — an external claim is advisory. A provider naming a role does not grant one,
                // so only what this platform states or derived itself reaches the token.
                Claims = context.Claims.Claims
                    .Where(c => c.Source is ClaimSource.Local or ClaimSource.Derived)
                    .GroupBy(c => c.Type, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.First().Value, StringComparer.Ordinal),
            },
            cancellationToken).ConfigureAwait(false);

        if (issued.IsFailure)
            return issued.ToNewResult<FlowResult>();

        RunnerLog.FlowCompleted(_logger, flow.Name,
            string.Join(", ", context.AchievedMethods), context.AchievedAcr ?? "none");

        return GenericResult<FlowResult>.Success(new FlowResult.Completed(issued.Value!));
    }
}
