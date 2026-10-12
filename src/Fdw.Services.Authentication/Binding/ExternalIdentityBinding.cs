using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Configuration;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Binding;

/// <summary>
/// Resolves an external subject to a local principal by reading <c>auth.ExternalIdentity</c>.
/// </summary>
/// <remarks>
/// <see cref="Resolve"/> matches on the pair and nothing else. There is no general email path, no
/// display-name path and no fall-through to a fuzzy match — an authenticated stranger with no row is
/// unbound, which is a decision for provisioning policy rather than something to guess at. The one
/// address match is <see cref="RedeemInvitation"/>: an operator-written row for one issuer and one
/// address, redeemed once, and only by a flow that opted in (FDW-910).
/// </remarks>
public sealed class ExternalIdentityBinding : IPrincipalBinding
{
    private const string ExternalIdentityDomain = "ExternalIdentity";

    private readonly IExternalIdentityConfigurationProvider _identities;
    private readonly ITenantResolver _tenants;
    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly ILogger<ExternalIdentityBinding> _logger;

    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityBinding"/> class.</summary>
    /// <param name="identities">Reads the binding rows.</param>
    /// <param name="tenants">Supplies the tenant a user belongs to.</param>
    /// <param name="authContextAccessor">Establishes the trusted context for pre-authentication reads.</param>
    /// <param name="logger">The logger.</param>
    public ExternalIdentityBinding(
        IExternalIdentityConfigurationProvider identities,
        ITenantResolver tenants,
        IAuthenticationContextAccessor authContextAccessor,
        ILogger<ExternalIdentityBinding>? logger = null)
    {
        _identities = identities ?? throw new ArgumentNullException(nameof(identities));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _logger = logger ?? NullLogger<ExternalIdentityBinding>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<Principal?>> Resolve(
        string issuer, string subjectId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subjectId))
            return GenericResult<Principal?>.Failure(BindingLog.LookupIncomplete(_logger));

        // This platform authentication lookup precedes the request authentication context.
        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        var all = await _identities.Get(cancellationToken).ConfigureAwait(false);
        if (all.IsFailure)
            return all.ToNewResult<Principal?>();

        var matches = (all.Value ?? [])
            .Where(i => i.IsActive
                && string.Equals(i.Issuer, issuer, StringComparison.Ordinal)
                && string.Equals(i.ExternalSubject, subjectId, StringComparison.Ordinal))
            .ToList();

        if (matches.Count == 0)
        {
            BindingLog.Unbound(_logger, issuer);
            return GenericResult<Principal?>.Success(null);
        }

        if (matches.Count > 1)
        {
            return GenericResult<Principal?>.Failure(
                BindingLog.Ambiguous(_logger, issuer, matches.Count));
        }

        var tenant = await _tenants.TenantFor(matches[0].UserId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
            return tenant.ToNewResult<Principal?>();

        BindingLog.Bound(_logger, issuer, matches[0].UserId);

        await StampFirstUse(matches[0], cancellationToken).ConfigureAwait(false);

        return GenericResult<Principal?>.Success(
            new Principal { Id = matches[0].UserId, TenantId = tenant.Value });
    }

    /// <inheritdoc />
    public async Task<IGenericResult<Principal?>> RedeemInvitation(
        string issuer, string subjectId, string verifiedEmail, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subjectId) || string.IsNullOrWhiteSpace(verifiedEmail))
            return GenericResult<Principal?>.Failure(BindingLog.LookupIncomplete(_logger));

        // This platform authentication lookup precedes the request authentication context.
        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        var all = await _identities.Get(cancellationToken).ConfigureAwait(false);
        if (all.IsFailure)
            return all.ToNewResult<Principal?>();

        // Open invitations only: a row that already names its subject was redeemed, and matching it
        // again by address would let a second person's account take the first one's place.
        var invitations = (all.Value ?? [])
            .OfType<ExternalIdentityConfiguration>()
            .Where(i => i.IsActive
                && i.ExternalSubject is null
                && string.Equals(i.Issuer, issuer, StringComparison.Ordinal)
                && string.Equals(i.InvitedEmail, verifiedEmail, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (invitations.Count == 0)
        {
            BindingLog.NoInvitation(_logger, issuer);
            return GenericResult<Principal?>.Success(null);
        }

        if (invitations.Count > 1)
            return GenericResult<Principal?>.Failure(BindingLog.Ambiguous(_logger, issuer, invitations.Count));

        var invitation = invitations[0];
        var tenant = await _tenants.TenantFor(invitation.UserId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
            return tenant.ToNewResult<Principal?>();

        // Redeemed in the same write that proves it: the subject is stated, so every later sign-in
        // resolves through Resolve by the pair and the address is never consulted again. A failed
        // write refuses the sign-in -- unlike a first-use stamp, this write IS the binding.
        invitation.ExternalSubject = subjectId;
        invitation.BoundAt = DateTimeOffset.UtcNow;
        var saved = await _identities.Save(
            invitation, ExternalIdentityDomain, ExternalIdentityDomain, invitation.Name, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure)
            return GenericResult<Principal?>.Failure(BindingLog.InvitationNotRedeemed(_logger, issuer, invitation.UserId));

        BindingLog.InvitationRedeemed(_logger, issuer, invitation.UserId);
        return GenericResult<Principal?>.Success(new Principal { Id = invitation.UserId, TenantId = tenant.Value });
    }

    /// <summary>
    /// Records the first sign-in this binding resolved.
    /// </summary>
    /// <remarks>
    /// A binding an administrator writes ahead of time is real but unproven: nobody has yet arrived
    /// from the provider carrying that subject. Stamping the first arrival is what lets a console
    /// tell "set up" from "in use", and it is stamped once — a later sign-in leaves it alone, so the
    /// value keeps meaning first use rather than last.
    ///
    /// A failure here is logged and swallowed deliberately: the sign-in has already been decided,
    /// and refusing an authenticated caller because a timestamp would not write is a worse answer
    /// than a binding that still reads as pending.
    /// </remarks>
    private async Task StampFirstUse(
        IExternalIdentityImplementationConfiguration binding, CancellationToken cancellationToken)
    {
        if (binding is not ExternalIdentityConfiguration configuration || configuration.BoundAt is not null)
            return;

        configuration.BoundAt = DateTimeOffset.UtcNow;

        var saved = await _identities.Save(
            configuration,
            ExternalIdentityDomain,
            ExternalIdentityDomain,
            configuration.Name,
            cancellationToken).ConfigureAwait(false);

        if (saved.IsFailure)
            BindingLog.FirstUseNotRecorded(_logger, configuration.Issuer);
    }
}
