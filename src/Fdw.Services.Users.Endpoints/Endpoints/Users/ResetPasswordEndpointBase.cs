using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Users;
using Fdw.Services.Users.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Abstract base class for resetting a user's password (POST /users/{UserId}/reset-password).
/// This is an admin operation requiring elevated permissions.
/// </summary>
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
    /// <param name="loggerFactory">The logger factory.</param>
    protected ResetPasswordEndpointBase(
        UserConfigurationProvider userProvider,
        IUserCredentialService credentialService,
        ILoggerFactory loggerFactory)
    {
        _userProvider = userProvider;
        _credentialService = credentialService;
        _logger = loggerFactory.CreateLogger(GetType());
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
        try
        {
            var userResult = await _userProvider.ResolveUser(req.IdOrName, ct).ConfigureAwait(false);
            if (!userResult.IsSuccess || userResult.Value is null)
            {
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

            var userId = userResult.Value.Id;

            // Why: Admin reset — store new password directly via IUserCredentialService without
            // verifying the old one. Hashing happens at the service boundary.
            var storeResult = await _credentialService.Store(userId, "Password", req.NewPassword, ct).ConfigureAwait(false);
            if (!storeResult.IsSuccess)
            {
                AddError("Failed to reset password.");
                await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
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
