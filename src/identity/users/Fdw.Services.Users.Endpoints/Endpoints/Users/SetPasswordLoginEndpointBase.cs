using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Users.Abstractions;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Generic base endpoint giving an account its FIRST password.
/// </summary>
/// <remarks>
/// An account created with only an API key has no way for its owner to sign in and mint their own
/// keys. This grants one. A platform administrator may always do it; the account's home-tenant
/// administrator may do it only while no password exists, because replacing one is a reset — taking
/// over a working login — and that stays with the platform administrator.
/// </remarks>
public abstract class SetPasswordLoginEndpointBase : Endpoint<SetPasswordLoginRequest>
{
    private readonly UserConfigurationProvider _userProvider;
    private readonly IUserCredentialService _credentialService;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="SetPasswordLoginEndpointBase"/> class.</summary>
    protected SetPasswordLoginEndpointBase(
        ILogger logger,
        UserConfigurationProvider userProvider,
        IUserCredentialService credentialService)
    {
        _logger = logger;
        _userProvider = userProvider;
        _credentialService = credentialService;
    }

    /// <summary>Gets the RBAC policy required by this endpoint. Defaults to "users:write".</summary>
    protected virtual string WritePolicy => "users:write";

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/users/{IdOrName}/password-login");
        Policies(WritePolicy);
        Summary(s =>
        {
            s.Summary = "Give an account its first password";
            s.Description = "Sets an initial password, marked must-change. Allowed for a platform administrator, and for the account's home-tenant administrator while the account has no password.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or further configuration.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc />
    public override async Task HandleAsync(SetPasswordLoginRequest req, CancellationToken ct)
    {
        var userResult = await UserRouteTarget
            .Resolve(_userProvider, User, req.IdOrName, ct).ConfigureAwait(false);

        // Row-level security hides accounts outside the caller's tenant, so "not found" also covers
        // "not yours to see".
        if (!userResult.IsSuccess || userResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var user = userResult.Value;
        var userId = user.Id;

        var roles = Resolve<ISystemRoleConfiguration>();
        var isPlatformAdmin = roles.IsPlatformAdmin(User);

        if (!AccountAuthority.Administers(User, user, roles))
        {
            UserEndpointLog.PasswordLoginNotPermitted(_logger, req.IdOrName);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        // A password nobody can sign in with is a credential that should not exist (FDW-910).
        var usable = await PasswordAdmission.Check(Resolve<ISignInMethodAdmission>(), userId, ct).ConfigureAwait(false);
        if (usable.IsFailure)
        {
            UserEndpointLog.PasswordAdmissionUndecided(_logger, req.IdOrName);
            AddError("Whether the account may sign in with a password could not be decided.");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        if (!usable.Value)
        {
            UserEndpointLog.PasswordNotUsable(_logger, "set a first password", req.IdOrName);
            AddError("No enabled password sign-in flow admits this account; grant it the permission the flow requires first.");
            await Send.ErrorsAsync(409, ct).ConfigureAwait(false);
            return;
        }

        var hasPassword = await _credentialService.HasCredential(userId, "Password", ct).ConfigureAwait(false);
        if (hasPassword.IsFailure)
        {
            AddError("The account's password state could not be read.");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        if (hasPassword.Value && !isPlatformAdmin)
        {
            UserEndpointLog.PasswordLoginAlreadySet(_logger, req.IdOrName);
            AddError("A password is already set; only a platform administrator may reset it.");
            await Send.ErrorsAsync(403, ct).ConfigureAwait(false);
            return;
        }

        var storeResult = await _credentialService
            .Store(userId, "Password", req.InitialPassword, ct).ConfigureAwait(false);
        if (!storeResult.IsSuccess)
        {
            AddError("The password could not be set.");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        // Why the flag after the store: Store clears it, and a password an administrator chose is
        // not one the account's owner has chosen yet.
        var forceResult = await _credentialService.ForcePasswordChange(userId, ct).ConfigureAwait(false);
        if (!forceResult.IsSuccess)
        {
            AddError("The password was set but could not be marked for change at next sign-in.");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        UserEndpointLog.PasswordLoginGranted(_logger, req.IdOrName);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
