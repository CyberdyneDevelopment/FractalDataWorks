using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Users.Abstractions;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Results;

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
    private readonly IUserConfigurationProvider _userProvider;
    private readonly IUserCredentialService _credentialService;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="SetPasswordLoginEndpointBase"/> class.</summary>
    protected SetPasswordLoginEndpointBase(
        ILogger logger,
        IUserConfigurationProvider userProvider,
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
        if (userResult.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(userResult, HttpContext)).ConfigureAwait(false);
            return;
        }

        if (userResult.Value is null)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(UserEndpointLog.SetPasswordLoginUserNotFound(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var user = userResult.Value;
        var userId = user.Id;

        var roles = Resolve<ISystemRoleConfiguration>();
        var isPlatformAdmin = roles.IsPlatformAdmin(User);

        if (!AccountAuthority.Administers(User, user, roles))
        {
            UserEndpointLog.PasswordLoginNotPermitted(_logger, req.IdOrName);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(UserEndpointLog.SetPasswordLoginForbidden(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        // A password nobody can sign in with is a credential that should not exist (FDW-910).
        var usable = await PasswordAdmission.Check(Resolve<ISignInMethodAdmission>(), userId, ct).ConfigureAwait(false);
        if (usable.IsFailure)
        {
            UserEndpointLog.PasswordAdmissionUndecided(_logger, req.IdOrName);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(usable, HttpContext)).ConfigureAwait(false);
            return;
        }

        if (!usable.Value)
        {
            UserEndpointLog.PasswordNotUsable(_logger, "set a first password", req.IdOrName);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(UserEndpointLog.SetPasswordLoginNoEnabledPasswordSignIn(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var hasPassword = await _credentialService.HasCredential(userId, "Password", ct).ConfigureAwait(false);
        if (hasPassword.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(hasPassword, HttpContext)).ConfigureAwait(false);
            return;
        }

        if (hasPassword.Value && !isPlatformAdmin)
        {
            UserEndpointLog.PasswordLoginAlreadySet(_logger, req.IdOrName);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(UserEndpointLog.SetPasswordLoginAPasswordIsAlreadySet(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var storeResult = await _credentialService
            .Store(userId, "Password", req.InitialPassword, ct).ConfigureAwait(false);
        if (!storeResult.IsSuccess)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(storeResult.Messages.Append(UserEndpointLog.SetPasswordLoginThePasswordCouldNotBe(_logger))), HttpContext)).ConfigureAwait(false);
            return;
        }

        // Why the flag after the store: Store clears it, and a password an administrator chose is
        // not one the account's owner has chosen yet.
        var forceResult = await _credentialService.ForcePasswordChange(userId, ct).ConfigureAwait(false);
        if (!forceResult.IsSuccess)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(forceResult, HttpContext)).ConfigureAwait(false);
            return;
        }

        UserEndpointLog.PasswordLoginGranted(_logger, req.IdOrName);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
