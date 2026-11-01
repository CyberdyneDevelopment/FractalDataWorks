using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Execution;

/// <summary>
/// The JSON-serializable shape of an <see cref="AuthenticationContext"/> — what gets encrypted into
/// <see cref="AuthenticationExecutionEntity.ContextData"/>.
/// </summary>
/// <remarks>
/// <see cref="AchievedMethods"/>/<see cref="AchievedAcr"/> mirror <c>AuthenticationContext</c>'s own
/// internal-init properties. Rebuilding them requires the internal object-initializer access
/// <c>Fdw.Services.Authentication</c> already has via <c>InternalsVisibleTo</c> on
/// <c>Fdw.Services.Authentication.Abstractions</c> — see <see cref="ToContext"/>. Everything else on
/// <c>AuthenticationContext</c> (<c>Subject</c>/<c>Principal</c>/<c>Decision</c>) is a plain public
/// record and serializes directly; only <c>Claim.Source</c> (an interface) needs the
/// <see cref="ClaimData"/> translation.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class AuthenticationContextData
{
    /// <summary>Gets or sets the proved subject, if any.</summary>
    public Subject? Subject { get; set; }

    /// <summary>Gets or sets the resolved local principal, if any.</summary>
    public Principal? Principal { get; set; }

    /// <summary>Gets or sets the claims gathered so far.</summary>
    public ClaimData[] Claims { get; set; } = [];

    /// <summary>Gets or sets the issuance decision, if any.</summary>
    public Decision? Decision { get; set; }

    /// <summary>Gets or sets the authentication methods proved so far — RFC 8176 values.</summary>
    public string[] AchievedMethods { get; set; } = [];

    /// <summary>Gets or sets the assurance level the achieved methods amount to.</summary>
    public string? AchievedAcr { get; set; }

    /// <summary>Gets or sets when the claims gathered so far stop being true.</summary>
    public DateTimeOffset? ClaimsValidUntil { get; set; }

    /// <summary>Builds the serializable shape of <paramref name="context"/>.</summary>
    /// <param name="context">The context to translate.</param>
    public static AuthenticationContextData FromContext(AuthenticationContext context)
        => new()
        {
            Subject = context.Subject,
            Principal = context.Principal,
            Claims = [.. context.Claims.Claims.Select(c => new ClaimData
            {
                Type = c.Type,
                Value = c.Value,
                Source = c.Source.Name,
                Issuer = c.Issuer,
            })],
            Decision = context.Decision,
            AchievedMethods = [.. context.AchievedMethods],
            AchievedAcr = context.AchievedAcr,
            ClaimsValidUntil = context.ClaimsValidUntil,
        };

    /// <summary>Rebuilds the real <see cref="AuthenticationContext"/> this data represents.</summary>
    /// <param name="logger">The logger.</param>
    public IGenericResult<AuthenticationContext> ToContext(ILogger<AuthenticationContextData>? logger = null)
    {
        logger ??= NullLogger<AuthenticationContextData>.Instance;

        var claims = new Claim[Claims.Length];
        for (var i = 0; i < Claims.Length; i++)
        {
            var source = ClaimSources.ByName(Claims[i].Source);
            if (source == ClaimSources.NotFound)
                return GenericResult<AuthenticationContext>.Failure(
                    ExecutionStoreLog.UnknownClaimSource(logger, Claims[i].Source));

            claims[i] = new Claim
            {
                Type = Claims[i].Type,
                Value = Claims[i].Value,
                Source = (IClaimSource)source,
                Issuer = Claims[i].Issuer,
            };
        }

        return GenericResult<AuthenticationContext>.Success(new AuthenticationContext
        {
            Subject = Subject,
            Principal = Principal,
            Claims = ClaimSet.Empty.Add(claims),
            Decision = Decision,
            AchievedMethods = [.. AchievedMethods],
            AchievedAcr = AchievedAcr,
            ClaimsValidUntil = ClaimsValidUntil,
        });
    }
}
