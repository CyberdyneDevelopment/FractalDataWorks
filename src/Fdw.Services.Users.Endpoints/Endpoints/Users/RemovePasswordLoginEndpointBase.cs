using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Users.Abstractions;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Generic base endpoint removing an account's password login.
/// </summary>
/// <remarks>
/// <para>
/// The counterpart of <see cref="SetPasswordLoginEndpointBase"/>: an operator who onboarded somebody
/// with a password and then moved them to single sign-on or an API key takes the password away here.
/// Before FDW-910 only the agent-key bootstrap could deactivate a password.
/// </para>
/// <para>
/// Whoever administers the account may do it — the platform administrator or the account's home-tenant
/// administrator. Removing a way in cannot take over an account, so it does not need the platform-only
/// authority a reset does. The account keeps every other way in; removing its last one leaves an
/// account nobody can sign in to, which is a legitimate thing for an operator to want.
/// </para>
/// </remarks>
public abstract class RemovePasswordLoginEndpointBase : Endpoint<UserScopedRequest>
{
    private readonly UserConfigurationProvider _userProvider;
    private readonly IUserCredentialService _credentialService;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="RemovePasswordLoginEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userProvider">Resolves the account.</param>
    /// <param name="credentialService">Removes the password.</param>
    protected RemovePasswordLoginEndpointBase(
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
        Delete("/users/{IdOrName}/password-login");
        Policies(WritePolicy);
        Summary(s =>
        {
            s.Summary = "Remove an account's password login";
            s.Description = "Disables the account's password so it can no longer sign in with one. Other sign-in methods are untouched. Allowed for a platform administrator and for the account's home-tenant administrator.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or further configuration.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc />
    public override async Task HandleAsync(UserScopedRequest req, CancellationToken ct)
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

        if (!AccountAuthority.Administers(User, user, Resolve<ISystemRoleConfiguration>()))
        {
            UserEndpointLog.PasswordRemovalNotPermitted(_logger, req.IdOrName);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var hasPassword = await _credentialService.HasCredential(user.Id, "Password", ct).ConfigureAwait(false);
        if (hasPassword.IsFailure)
        {
            AddError("The account's password state could not be read.");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        if (!hasPassword.Value)
        {
            UserEndpointLog.PasswordLoginAbsent(_logger, req.IdOrName);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var removed = await _credentialService.Remove(user.Id, "Password", ct).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            AddError("The password could not be removed.");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        UserEndpointLog.PasswordLoginRemoved(_logger, req.IdOrName);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
