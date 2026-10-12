using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Users;
using Fdw.Services.Users.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Abstract base class for resetting a user's password (POST /users/{UserId}/reset-password).
/// </summary>
/// <remarks>
/// <para>
/// Only the PLATFORM administrator may do this — an Admin acting in the global tenant. A tenant's
/// admin never sets anyone's password (FDW-825); they can require the user to change it at next
/// login instead (<see cref="RequirePasswordChangeEndpointBase"/>). Before this, any holder of
/// <c>users:write</c> in any tenant could set any user's password, the global admin's included.
/// </para>
/// <para>
/// The password set here is a temporary one: the account is marked to change it at next login, so
/// the user replaces it with one only they know. This is how a user who has forgotten their
/// password gets back in.
/// </para>
/// </remarks>
public abstract class ResetPasswordEndpointBase : Endpoint<ResetPasswordRequest>
{
    private readonly UserConfigurationProvider _userProvider;
    private readonly IUserCredentialService _credentialService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResetPasswordEndpointBase"/> class.
    /// </summary>
    /// <param name="userProvider">The user configuration provider.</param>
    /// <param name="credentialService">The user credential service.</param>
    /// <param name="logger">The logger.</param>
    protected ResetPasswordEndpointBase(
        UserConfigurationProvider userProvider,
        IUserCredentialService credentialService,
        ILogger logger)
    {
        _userProvider = userProvider;
        _credentialService = credentialService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the user provider.
    /// </summary>
    protected UserConfigurationProvider UserProvider => _userProvider;

    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger EndpointLogger => _logger;

    /// <summary>
    /// Gets the RBAC policy required by this endpoint. Defaults to "users:write".
    /// </summary>
    protected virtual string WritePolicy => "users:write";

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/users/{IdOrName}/reset-password");
        Policies(WritePolicy);
        Summary(s =>
        {
            s.Summary = "Reset a user's password";
            s.Description = "Resets the specified user's password. Requires admin privileges.";
        });
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ResetPasswordRequest req, CancellationToken ct)
    {
        if (!Resolve<ISystemRoleConfiguration>().IsPlatformAdmin(User))
        {
            UserEndpointLog.ResetPasswordRequiresPlatformAdmin(_logger, req.IdOrName);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        try
        {
            var userResult = Guid.TryParse(req.IdOrName, out var requestedId)
            ? await _userProvider.Get(requestedId, ct).ConfigureAwait(false)
            : await _userProvider.Get(req.IdOrName, ct).ConfigureAwait(false);
            if (!userResult.IsSuccess || userResult.Value is null)
            {
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

            // Credentials key on the user's durable id -- the Users domain row's -- not on the
            // implementation row's own Id; the change-password path keys on the same one.
            var userId = userResult.Value.Id;

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
                UserEndpointLog.PasswordNotUsable(_logger, "reset the password", req.IdOrName);
                AddError("No enabled password sign-in flow admits this account; grant it the permission the flow requires first.");
                await Send.ErrorsAsync(409, ct).ConfigureAwait(false);
                return;
            }

            var storeResult = await _credentialService.Store(userId, "Password", req.NewPassword, ct).ConfigureAwait(false);
            if (!storeResult.IsSuccess)
            {
                AddError("Failed to reset password.");
                await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
                return;
            }

            // Store clears the must-change mark, so it is set again afterwards: the password an
            // administrator chose is temporary, and the user replaces it at their next sign-in.
            var forceResult = await _credentialService.ForcePasswordChange(userId, ct).ConfigureAwait(false);
            if (!forceResult.IsSuccess)
            {
                AddError("The password was reset but could not be marked for change at next login.");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            await Send.NoContentAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            UserEndpointLog.OperationFailed(_logger, ex, "reset-password", req.IdOrName);
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }
}
