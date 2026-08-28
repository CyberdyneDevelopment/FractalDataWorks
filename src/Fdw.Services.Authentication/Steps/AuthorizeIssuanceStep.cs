using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Steps;

/// <summary>
/// Decides whether this principal may be issued a token at all.
/// </summary>
/// <remarks>
/// The login-time question — is the account enabled, the tenant active. Not the per-request question
/// of whether a principal may perform some action on some object, which happens thousands of times
/// per second against a live decision point and is never part of a flow. The flow ends at issuance;
/// everything after the token belongs to the resource.
/// </remarks>
public sealed class AuthorizeIssuanceStep : IAuthenticationStep
{
    private readonly IIssuanceEligibility _eligibility;
    private readonly ILogger<AuthorizeIssuanceStep> _logger;

    /// <summary>Initializes a new instance of the <see cref="AuthorizeIssuanceStep"/> class.</summary>
    /// <param name="eligibility">Answers whether a principal may hold a token.</param>
    /// <param name="logger">The logger.</param>
    public AuthorizeIssuanceStep(IIssuanceEligibility eligibility, ILogger<AuthorizeIssuanceStep>? logger = null)
    {
        _eligibility = eligibility ?? throw new ArgumentNullException(nameof(eligibility));
        _logger = logger ?? NullLogger<AuthorizeIssuanceStep>.Instance;
    }

    /// <inheritdoc />
    public IReadOnlyList<ContextElement> Requires => [ContextElement.Principal];

    /// <inheritdoc />
    public IReadOnlyList<ContextElement> Contributes => [ContextElement.Decision];

    /// <inheritdoc />
    /// <remarks>Deciding is not proving. Nothing about the caller is established here.</remarks>
    public IReadOnlyList<string> AuthenticationMethods => [];

    /// <inheritdoc />
    public async Task<IGenericResult<StepOutcome>> Execute(
        AuthenticationContext context, CancellationToken cancellationToken = default)
    {
        var principal = context.Principal!;

        var decision = await _eligibility
            .MayBeIssued(principal, cancellationToken)
            .ConfigureAwait(false);

        if (decision.IsFailure)
            return decision.ToNewResult<StepOutcome>();

        // Why a denial is contributed rather than returned as a failure: the runner's terminal check
        // refuses on a decision that does not permit, and it does so with the reason this carries. A
        // failure here would lose that reason and report the step as broken rather than the login as
        // denied.
        StepLog.EligibilityDecided(_logger, principal.Id, decision.Value!.Permitted, decision.Value!.Reason);

        return GenericResult<StepOutcome>.Success(
            new StepOutcome.Contributed(new ContextContribution { Decision = decision.Value }));
    }
}
