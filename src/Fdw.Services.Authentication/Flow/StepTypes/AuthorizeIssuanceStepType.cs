using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Flow.StepTypes;

/// <summary>
/// Decides whether a token may be issued for the established principal.
/// </summary>
/// <remarks>
/// Last in a flow, because it decides on what everything before it established. The option IS the
/// step: a flow names it, the collection answers by that name, and what answers is what runs.
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(AuthenticationStepTypes), "AuthorizeIssuance")]
public sealed class AuthorizeIssuanceStepType
    : AuthenticationStepTypeBase<IGenericService, IServiceFactory<IGenericService, IServiceConfiguration>>,
      IAuthenticationStep
{
    // Captured when the host is built: an option is created by its module initializer, which needs
    // a parameterless constructor, so what it needs arrives where a live container exists.
    private IIssuanceEligibility? _eligibility;
    private ILogger _logger = NullLogger<AuthorizeIssuanceStepType>.Instance;

    /// <summary>Initializes a new instance of the <see cref="AuthorizeIssuanceStepType"/> class.</summary>
    public AuthorizeIssuanceStepType()
        : base("AuthorizeIssuance",
               "AuthenticationSteps",
               "Authorize Issuance",
               "Decides whether a token may be issued for the established principal")
    {
        Initialization((host, loggerFactory) =>
        {
            _eligibility = host.Services.GetRequiredService<IIssuanceEligibility>();
            _logger = loggerFactory?.CreateLogger<AuthorizeIssuanceStepType>()
                ?? NullLogger<AuthorizeIssuanceStepType>.Instance;

            return GenericResult<IHost>.Success(host);
        });
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
        // Refusing is the only safe reading of a missing eligibility check: the alternative is
        // issuing a token because nothing was there to say no.
        if (_eligibility is null)
            return GenericResult<StepOutcome>.Failure(StepLog.NotInitialized(_logger, Name));

        var principal = context.Principal!;

        var decision = await _eligibility
            .MayBeIssued(principal, cancellationToken)
            .ConfigureAwait(false);

        if (decision.IsFailure)
            return decision.ToNewResult<StepOutcome>();

        StepLog.EligibilityDecided(_logger, principal.Id, decision.Value!.Permitted, decision.Value!.Reason);

        return GenericResult<StepOutcome>.Success(
            new StepOutcome.Contributed(new ContextContribution { Decision = decision.Value }));
    }
}
