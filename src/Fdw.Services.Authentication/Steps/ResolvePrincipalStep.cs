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
/// Turns a subject an authority asserted into a principal this platform knows.
/// </summary>
/// <remarks>
/// <para>
/// The federation boundary. However much of authentication is delegated, this stays internal — no
/// external party can say which of our principals an external (issuer, subject) pair belongs to.
/// </para>
/// <para>
/// Looks up on the pair, never on an email address: an address is frequently unverified, it changes,
/// and two issuers can assert the same one for different people. Matching on it is the classic
/// account-takeover path in federated login.
/// </para>
/// </remarks>
public sealed class ResolvePrincipalStep : IAuthenticationStep
{
    private readonly IPrincipalBinding _bindings;
    private readonly ILogger<ResolvePrincipalStep> _logger;

    /// <summary>Initializes a new instance of the <see cref="ResolvePrincipalStep"/> class.</summary>
    /// <param name="bindings">Maps an external subject to a local principal.</param>
    /// <param name="logger">The logger.</param>
    public ResolvePrincipalStep(IPrincipalBinding bindings, ILogger<ResolvePrincipalStep>? logger = null)
    {
        _bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        _logger = logger ?? NullLogger<ResolvePrincipalStep>.Instance;
    }

    /// <inheritdoc />
    public IReadOnlyList<ContextElement> Requires => [ContextElement.Subject];

    /// <inheritdoc />
    public IReadOnlyList<ContextElement> Contributes => [ContextElement.Principal];

    /// <inheritdoc />
    /// <remarks>Resolution proves nothing — the subject was already proved by whatever ran before.</remarks>
    public IReadOnlyList<string> AuthenticationMethods => [];

    /// <inheritdoc />
    public async Task<IGenericResult<StepOutcome>> Execute(
        AuthenticationContext context, CancellationToken cancellationToken = default)
    {
        var subject = context.Subject!;

        var bound = await _bindings
            .Resolve(subject.Issuer, subject.SubjectId, cancellationToken)
            .ConfigureAwait(false);

        if (bound.IsFailure)
            return bound.ToNewResult<StepOutcome>();

        if (bound.Value is null)
        {
            // Why a failure and not a silent skip: an authenticated stranger with no binding is
            // exactly the case provisioning policy exists to decide. Contributing nothing here would
            // let the flow continue and fail later somewhere less clear about why.
            return GenericResult<StepOutcome>.Failure(StepLog.NoBinding(_logger, subject.Issuer));
        }

        StepLog.PrincipalResolved(_logger, subject.Issuer, bound.Value.Id);

        return GenericResult<StepOutcome>.Success(
            new StepOutcome.Contributed(new ContextContribution { Principal = bound.Value }));
    }
}
