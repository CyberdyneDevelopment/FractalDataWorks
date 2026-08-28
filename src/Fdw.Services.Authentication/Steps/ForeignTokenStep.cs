using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Fdw.Services.Authentication.Steps;

/// <summary>
/// Accepts a token an external authority already issued, and turns it into a subject.
/// </summary>
/// <remarks>
/// <para>
/// The step for the case where the caller — usually a browser app — went to the identity provider
/// itself and arrives holding the result. It verifies that token and nothing more: the claims inside
/// it are the provider's assertions, not authority here, so they arrive marked
/// <see cref="ClaimSource.External"/> and change no decision without an explicit mapping.
/// </para>
/// <para>
/// This is the platform side of RFC 8693 token exchange — a foreign token in, one of ours out — so a
/// flow using it should be reachable at the token endpoint's exchange grant rather than through a
/// bespoke endpoint.
/// </para>
/// </remarks>
public sealed class ForeignTokenStep : IAuthenticationStep
{
    private readonly ForeignTokenStepConfiguration _configuration;
    private readonly ISigningKeyProvider _keys;
    private readonly IForeignTokenAccessor _presented;
    private readonly ILogger<ForeignTokenStep> _logger;

    /// <summary>Initializes a new instance of the <see cref="ForeignTokenStep"/> class.</summary>
    /// <param name="configuration">Which authority to trust and on what terms.</param>
    /// <param name="keys">Supplies that authority's current signing keys.</param>
    /// <param name="presented">Supplies the token the caller presented.</param>
    /// <param name="logger">The logger.</param>
    public ForeignTokenStep(
        ForeignTokenStepConfiguration configuration,
        ISigningKeyProvider keys,
        IForeignTokenAccessor presented,
        ILogger<ForeignTokenStep>? logger = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        _presented = presented ?? throw new ArgumentNullException(nameof(presented));
        _logger = logger ?? NullLogger<ForeignTokenStep>.Instance;
    }

    /// <inheritdoc />
    public IReadOnlyList<ContextElement> Requires => [];

    /// <inheritdoc />
    public IReadOnlyList<ContextElement> Contributes => [ContextElement.Subject, ContextElement.Claims];

    /// <inheritdoc />
    public IReadOnlyList<string> AuthenticationMethods => _configuration.AssertableMethods;

    /// <inheritdoc />
    public async Task<IGenericResult<StepOutcome>> Execute(
        AuthenticationContext context, CancellationToken cancellationToken = default)
    {
        var token = _presented.Token;
        if (string.IsNullOrWhiteSpace(token))
            return GenericResult<StepOutcome>.Failure(
                ForeignTokenLog.NoTokenPresented(_logger, _configuration.Issuer));

        var keys = await _keys.Current(_configuration.JwksUri, cancellationToken).ConfigureAwait(false);
        if (keys.IsFailure)
            return keys.ToNewResult<StepOutcome>();

        var validated = await new JsonWebTokenHandler()
            .ValidateTokenAsync(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _configuration.Issuer,
                ValidateAudience = true,
                ValidAudiences = _configuration.ValidAudiences,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = keys.Value,
                ValidAlgorithms = _configuration.ValidAlgorithms,
                ClockSkew = _configuration.ClockSkew,
            })
            .ConfigureAwait(false);

        if (!validated.IsValid)
        {
            return GenericResult<StepOutcome>.Failure(
                ForeignTokenLog.Rejected(_logger, _configuration.Issuer,
                    validated.Exception?.GetType().Name ?? "unknown"));
        }

        var subjectId = validated.ClaimsIdentity.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subjectId))
            return GenericResult<StepOutcome>.Failure(
                ForeignTokenLog.NoSubjectClaim(_logger, _configuration.Issuer));

        ForeignTokenLog.Accepted(_logger, _configuration.Issuer);

        return GenericResult<StepOutcome>.Success(new StepOutcome.Contributed(new ContextContribution
        {
            ObservedMethods = [.. validated.ClaimsIdentity.FindAll("amr").Select(c => c.Value)],

            Subject = new Subject
            {
                Issuer = _configuration.Issuer,
                SubjectId = subjectId,
                AuthenticatedAt = DateTimeOffset.UtcNow,
            },
            Claims = [.. validated.ClaimsIdentity.Claims
                .Where(c => c.Type is not ("sub" or "aud" or "iss" or "exp" or "nbf" or "iat"))
                .Select(c => new Claim
                {
                    Type = c.Type,
                    Value = c.Value,
                    Source = ClaimSource.External,
                    Issuer = _configuration.Issuer,
                })],
        }));
    }
}
