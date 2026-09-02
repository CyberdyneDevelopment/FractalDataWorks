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
    private readonly IAcrPolicy _acrPolicy;
    private readonly ITokenIssuer _issuer;
    private readonly IAuthenticationExecutionStore _executions;
    private readonly IAuthenticationFlowProvider _flows;
    private readonly ILogger<AuthenticationRunner> _logger;

    // How a name becomes a step. Defaults to the collection, which is the registry — a delegate
    // rather than an injected service because there is nothing to register: the only reason it is
    // a parameter at all is that the runner's invariants are about what it does with a step's
    // declarations, and proving those needs steps that vary per test.
    private readonly Func<string, IAuthenticationStep?> _step;

    /// <summary>Initializes a new instance of the <see cref="AuthenticationRunner"/> class.</summary>
    /// <param name="acrPolicy">Turns proved methods into an assurance level.</param>
    /// <param name="issuer">Mints the token once the terminal check passes.</param>
    /// <param name="executions">Holds a suspended flow between the redirect and the return.</param>
    /// <param name="flows">
    /// Resolves the flow a suspended execution belongs to by the name it recorded at suspend time, so
    /// a caller resuming one need only hold the resume token — never a flow name it would otherwise
    /// have to carry across the round-trip itself (a query parameter, a route segment, a second
    /// cookie) purely to hand back to this runner.
    /// </param>
    /// <param name="step">
    /// How a name becomes a step. Supplied rather than defaulted: the registration hands over the
    /// collection, which is the registry, and a test hands over steps that vary per case — which is
    /// what proving this runner's invariants requires.
    /// </param>
    /// <param name="logger">The logger.</param>
    public AuthenticationRunner(
        IAcrPolicy acrPolicy,
        ITokenIssuer issuer,
        IAuthenticationExecutionStore executions,
        IAuthenticationFlowProvider flows,
        Func<string, IAuthenticationStep?> step,
        ILogger<AuthenticationRunner>? logger = null)
    {
        _acrPolicy = acrPolicy ?? throw new ArgumentNullException(nameof(acrPolicy));
        _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        _executions = executions ?? throw new ArgumentNullException(nameof(executions));
        _flows = flows ?? throw new ArgumentNullException(nameof(flows));
        _step = step ?? throw new ArgumentNullException(nameof(step));
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
    /// <param name="resumeToken">The token handed out when it suspended.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// The flow itself is resolved from what the execution recorded at suspend time, not supplied by
    /// the caller — a caller resuming an execution needs only the resume token. Requiring the flow
    /// too would mean every caller resuming any flow has to carry its name across whatever round-trip
    /// separates the challenge from the return, which for a provider-driven redirect is a value with
    /// nowhere neutral to ride: not the query string (reserved for the provider's own code/state) and
    /// not the URL (the route is fixed once and shared by every flow of that shape).
    /// </remarks>
    public async Task<IGenericResult<FlowResult>> Resume(
        string resumeToken, CancellationToken cancellationToken = default)
    {
        var consumed = await _executions.TryConsume(resumeToken, cancellationToken).ConfigureAwait(false);
        if (consumed.IsFailure)
            return consumed.ToNewResult<FlowResult>();

        var record = consumed.Value!;

        var flow = await _flows.Get(record.FlowName, cancellationToken).ConfigureAwait(false);
        if (flow.IsFailure || flow.Value is null)
            return GenericResult<FlowResult>.Failure(
                RunnerLog.ResumedFlowNotFound(_logger, record.Id, record.FlowName));

        RunnerLog.FlowResuming(_logger, flow.Value.Name, record.Id, record.CurrentStepIndex);

        return await Execute(flow.Value, record.Context, record.CurrentStepIndex, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IGenericResult<FlowResult>> Execute(
        AuthenticationFlow flow, AuthenticationContext context, int startAt, CancellationToken cancellationToken)
    {
        if (startAt == 0)
            RunnerLog.FlowStarting(_logger, flow.Name, flow.Steps.Count);

        for (var i = startAt; i < flow.Steps.Count; i++)
        {
            // The collection is the registry: an option joined it by declaring itself, and ByName
            // is how every other domain selects one. A name nothing answers to is a flow naming a
            // step whose package is not referenced.
            // The collection answers by name and what it returns IS the step. A flow that reached
            // here was validated when configuration loaded, so a miss now means the collection
            // changed under a cached flow rather than a mis-typed row.
            if (_step(flow.Steps[i]) is not { } step)
                return GenericResult<FlowResult>.Failure(
                    RunnerLog.StepNotAvailable(_logger, flow.Steps[i]));
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
            RunnerLog.UndeclaredContribution(_logger, stepName, element.Name);

        var declared = step.Contributes;

        return context with
        {
            Subject = declared.Contains(ContextElements.Subject) && contribution.Subject is not null
                ? contribution.Subject : context.Subject,
            Principal = declared.Contains(ContextElements.Principal) && contribution.Principal is not null
                ? contribution.Principal : context.Principal,
            Claims = declared.Contains(ContextElements.Claims) && contribution.Claims.Count > 0
                ? context.Claims.Add(contribution.Claims) : context.Claims,
            Decision = declared.Contains(ContextElements.Decision) && contribution.Decision is not null
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
                ExpiresAt = DateTimeOffset.UtcNow.Add(flow.ExecutionLifetime),
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
                //
                // Every value of a repeated type is carried, not the first: permissions and roles
                // arrive as many claims of one type, and keeping one would mint a token that
                // verifies and then refuses everything its holder is actually entitled to.
                Claims = context.Claims.Claims
                    .Where(c => c.Source == ClaimSources.Local || c.Source == ClaimSources.Derived)
                    .GroupBy(c => c.Type, StringComparer.Ordinal)
                    .ToDictionary(
                        g => g.Key,
                        g => (IReadOnlyList<string>)[.. g.Select(c => c.Value)],
                        StringComparer.Ordinal),
            },
            cancellationToken).ConfigureAwait(false);

        if (issued.IsFailure)
            return issued.ToNewResult<FlowResult>();

        RunnerLog.FlowCompleted(_logger, flow.Name,
            string.Join(", ", context.AchievedMethods), context.AchievedAcr ?? "none");

        return GenericResult<FlowResult>.Success(new FlowResult.Completed(issued.Value!));
    }
}
