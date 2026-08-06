using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Endpoints.Models;
using Microsoft.Extensions.Logging;
using Fdw.Services.Authentication.Clients.Models;

namespace Fdw.Services.Authentication.Endpoints;

/// <summary>
/// Abstract base class for changing the current user's password (POST /users/me/password).
/// </summary>
public abstract class ChangePasswordEndpointBase : Endpoint<ChangePasswordRequest>
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangePasswordEndpointBase"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    protected ChangePasswordEndpointBase(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(GetType());
    }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger EndpointLogger => _logger;

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/users/me/password");
        Summary(s =>
        {
            s.Summary = "Change current user's password";
            s.Description = "Changes the authenticated user's password after verifying the current password.";
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
    public override async Task HandleAsync(ChangePasswordRequest req, CancellationToken ct)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            AuthenticationEndpointLog.UserIdentityNotFound(_logger);
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        AuthenticationEndpointLog.PasswordChangeRequested(_logger, username);

        try
        {
            var result = await PerformChangePassword(username, req.CurrentPassword, req.NewPassword, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                AuthenticationEndpointLog.PasswordChangeFailed(_logger, username);
                AddError("Password change failed. Verify your current password is correct.");
                await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
                return;
            }

            AuthenticationEndpointLog.PasswordChanged(_logger, username);
            await Send.NoContentAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AuthenticationEndpointLog.AuthenticationException(_logger, ex, "change-password");
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Performs the password change operation. Implementers must override this to
    /// verify the current password and set the new password.
    /// </summary>
    /// <param name="username">The authenticated username.</param>
    /// <param name="currentPassword">The user's current password.</param>
    /// <param name="newPassword">The new password to set.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    protected abstract Task<IGenericResult> PerformChangePassword(
        string username, string currentPassword, string newPassword, CancellationToken ct);
}
