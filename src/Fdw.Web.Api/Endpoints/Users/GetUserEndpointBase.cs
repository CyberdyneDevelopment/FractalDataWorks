using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Generic base endpoint for getting a user by ID or username.
/// </summary>
public abstract class GetUserEndpointBase : Endpoint<UserScopedRequest, UserResponse>
{
    private readonly UserConfigurationProvider _userProvider;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    protected GetUserEndpointBase(UserConfigurationProvider userProvider)
    {
        _userProvider = userProvider;
    }

    /// <summary>
    /// Gets the user provider.
    /// </summary>
    protected UserConfigurationProvider UserProvider => _userProvider;

    /// <summary>
    /// Gets the RBAC policy required by this endpoint. Defaults to "users:read".
    /// </summary>
    protected virtual string ReadPolicy => "users:read";

    /// <inheritdoc />
    public override void Configure()
    {
        // API-66: route accepts Guid id OR username via IdOrName.
        Get("/users/{IdOrName}");
        Policies(ReadPolicy);
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

        var result = await _userProvider.ResolveUser(req.IdOrName, ct).ConfigureAwait(false);

        if (!result.IsSuccess || result.Value is null)
        {
            // API-62: structured 404 envelope.
            HttpContext.Response.StatusCode = 404;
            HttpContext.Response.ContentType = "application/json";
            await HttpContext.Response.WriteAsJsonAsync(new
            {
                errorCode = "NotFound",
                messages = new[] { $"User '{req.IdOrName}' was not found." }
            }, ct).ConfigureAwait(false);
            return;
        }

        var response = MapToResponse(result.Value);
        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps a user entity to a response DTO. Override for custom mapping.
    /// </summary>
    protected abstract UserResponse MapToResponse(IUser user);
}
