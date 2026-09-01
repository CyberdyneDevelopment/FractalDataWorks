using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Flow.StepTypes;

/// <summary>
/// Binds a subject an authority proved to a principal this platform knows.
/// </summary>
/// <remarks>
/// <para>
/// The half of federated login that the authority cannot do: it proved who the caller is to ITSELF,
/// and has no idea which of our principals that corresponds to. Every federated step therefore
/// contributes a Subject and stops, and this turns that into a Principal.
/// </para>
/// <para>
/// It stays a step of its own rather than folding into each authority's step, because the mapping
/// is the same wherever the subject came from — and because <c>Requires</c> Subject and
/// <c>Contributes</c> Principal is what stops a flow minting a token for nobody.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(AuthenticationStepTypes), "ResolvePrincipal")]
public sealed class ResolvePrincipalStepType
    : AuthenticationStepTypeBase<IGenericService, IServiceFactory<IGenericService, IServiceConfiguration>>,
      IAuthenticationStep
{
    // Captured when the host is built: an option is created by its module initializer, which needs
    // a parameterless constructor, so what it needs arrives where a live container exists.
    private IPrincipalBinding? _bindings;
    private ILogger _logger = NullLogger<ResolvePrincipalStepType>.Instance;

    /// <summary>Initializes a new instance of the <see cref="ResolvePrincipalStepType"/> class.</summary>
    public ResolvePrincipalStepType()
        : base("ResolvePrincipal",
               "AuthenticationSteps",
               "Resolve Principal",
               "Binds a subject an authority proved to a principal this platform knows")
    {
        Initialization((host, loggerFactory) =>
        {
            _bindings = host.Services.GetRequiredService<IPrincipalBinding>();
            _logger = loggerFactory?.CreateLogger<ResolvePrincipalStepType>()
                ?? NullLogger<ResolvePrincipalStepType>.Instance;

            return GenericResult<IHost>.Success(host);
        });
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
        if (_bindings is null)
            return GenericResult<StepOutcome>.Failure(StepLog.NotInitialized(_logger, Name));

        var subject = context.Subject!;

        var bound = await _bindings
            .Resolve(subject.Issuer, subject.SubjectId, cancellationToken)
            .ConfigureAwait(false);

        if (bound.IsFailure)
            return bound.ToNewResult<StepOutcome>();

        // A subject with no binding is a caller this platform has never seen. Refusing is the whole
        // point of the step: the alternative is inventing a principal for them.
        if (bound.Value is null)
            return GenericResult<StepOutcome>.Failure(StepLog.NoBinding(_logger, subject.Issuer));

        StepLog.PrincipalResolved(_logger, subject.Issuer, bound.Value.Id);

        return GenericResult<StepOutcome>.Success(
            new StepOutcome.Contributed(new ContextContribution { Principal = bound.Value }));
    }
}
