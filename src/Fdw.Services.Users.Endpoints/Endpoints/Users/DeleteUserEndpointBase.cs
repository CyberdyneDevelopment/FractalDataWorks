using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Generic base endpoint for deleting a user.
/// </summary>
public abstract class DeleteUserEndpointBase : Endpoint<UserScopedRequest>
{
    private readonly UserConfigurationProvider _userProvider;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    protected DeleteUserEndpointBase(UserConfigurationProvider userProvider)
    {
        _userProvider = userProvider;
    }

    /// <summary>
    /// Gets the user provider.
    /// </summary>
    protected UserConfigurationProvider UserProvider => _userProvider;

    /// <summary>
    /// Gets the RBAC policy required by this endpoint. Defaults to "users:delete".
    /// </summary>
    protected virtual string DeletePolicy => "users:delete";

    /// <inheritdoc />
    public override void Configure()
    {
        // API-66: route param is {IdOrName} — accepts either Guid id or username.
        Delete("/users/{IdOrName}");
        Policies(DeletePolicy);
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (auth, summary, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(UserScopedRequest req, CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        var lookup = await _userProvider.ResolveUser(req.IdOrName, ct).ConfigureAwait(false);
        if (!lookup.IsSuccess || lookup.Value is null)
        {
            HttpContext.Response.StatusCode = 404;
            HttpContext.Response.ContentType = "application/json";
            await HttpContext.Response.WriteAsJsonAsync(new
            {
                errorCode = "NotFound",
                messages = new[] { $"User '{req.IdOrName}' was not found." }
            }, ct).ConfigureAwait(false);
            return;
        }

        var userId = lookup.Value.Id;

        OnDeletingUser(userId);

        var result = await _userProvider.DeleteUser(userId, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            ThrowError(result.CurrentMessage ?? "Failed to delete user", 500);
            return;
        }

        OnUserDeleted(userId);

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Called when deleting a user. Override for custom logging.
    /// </summary>
    protected virtual void OnDeletingUser(Guid userId)
    {
    }

    /// <summary>
    /// Called when a user has been deleted. Override for custom logging.
    /// </summary>
    protected virtual void OnUserDeleted(Guid userId)
    {
    }
}
