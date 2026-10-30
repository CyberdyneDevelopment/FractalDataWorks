using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Generic base endpoint binding an external identity to an account before its first sign-in.
/// </summary>
/// <remarks>
/// Without a binding, the first sign-in through an issuer provisions a NEW account. Writing the
/// binding first is what makes that sign-in land on an account an administrator has already set up,
/// with its tenant and roles in place.
/// </remarks>
public abstract class BindExternalIdentityEndpointBase : Endpoint<BindExternalIdentityRequest>
{
    private const string ExternalIdentityDomain = "ExternalIdentity";

    private readonly UserConfigurationProvider _userProvider;
    private readonly IExternalIdentityConfigurationProvider _externalIdentities;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="BindExternalIdentityEndpointBase"/> class.</summary>
    protected BindExternalIdentityEndpointBase(
        ILogger logger,
        UserConfigurationProvider userProvider,
        IExternalIdentityConfigurationProvider externalIdentities)
    {
        _logger = logger;
        _userProvider = userProvider;
        _externalIdentities = externalIdentities;
    }

    /// <summary>Gets the RBAC policy required by this endpoint. Defaults to "users:write".</summary>
    protected virtual string WritePolicy => "users:write";

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/users/{IdOrName}/external-identities");
        Policies(WritePolicy);
        Summary(s =>
        {
            s.Summary = "Bind an external identity to an account";
            s.Description = "Binds an issuer's subject to this account so its first sign-in resolves here instead of provisioning a second account.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or further configuration.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc />
    public override async Task HandleAsync(BindExternalIdentityRequest req, CancellationToken ct)
    {
        var userResult = await UserRouteTarget
            .Resolve(_userProvider, User, req.IdOrName, ct).ConfigureAwait(false);

        if (!userResult.IsSuccess || userResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var user = userResult.Value;

        // Exactly one: a subject binds now, an address invites. Both, or neither, is a request that
        // cannot say which the operator meant.
        if (string.IsNullOrWhiteSpace(req.Subject) == string.IsNullOrWhiteSpace(req.InvitedEmail))
        {
            AddError("Name exactly one of subject (bind now) or invitedEmail (invite).");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        if (!AccountAuthority.Administers(User, user, Resolve<ISystemRoleConfiguration>()))
        {
            UserEndpointLog.ApiKeyOperationNotPermitted(_logger, req.IdOrName);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        // A binding is matched against the issuer a proved subject carries, so one naming an issuer
        // this deployment cannot resolve is inert: it writes, reads back, and the first real sign-in
        // ignores it and provisions a second account.
        var issuers = await Resolve<IExternalIssuerCatalog>().Read(ct).ConfigureAwait(false);
        if (issuers.IsFailure || issuers.Value is null)
        {
            AddError("The issuers this platform trusts could not be read.");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        if (!issuers.Value.Knows(req.Issuer))
        {
            // An incomplete set cannot justify a refusal: the issuer may be configured on the one
            // authority that would not answer. Saying "cannot determine" is the honest answer, and
            // it is retryable; saying "unknown" would not be.
            if (!issuers.Value.IsComplete)
            {
                UserEndpointLog.ExternalIssuersUnreadable(_logger, req.Issuer);
                AddError("Whether this platform can resolve that issuer could not be determined: an authority could not be read.");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            UserEndpointLog.ExternalIssuerUnknown(_logger, req.Issuer);
            AddError($"'{req.Issuer}' is not an issuer this platform can resolve. Accepted: {string.Join(", ", issuers.Value.Issuers)}.");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        var row = ExternalIdentityRow.For(user.Id, req.Issuer, req.Subject, req.InvitedEmail);
        var saveResult = await _externalIdentities.Create(row, ExternalIdentityDomain, ExternalIdentityDomain, row.Name, ct)
            .ConfigureAwait(false);

        if (!saveResult.IsSuccess)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(saveResult, HttpContext)).ConfigureAwait(false);
            return;
        }

        UserEndpointLog.ExternalIdentityBound(_logger, req.Issuer, req.IdOrName);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
