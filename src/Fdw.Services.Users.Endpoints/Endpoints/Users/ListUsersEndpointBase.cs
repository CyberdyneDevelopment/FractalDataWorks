using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Models;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Generic base endpoint for listing all users.
/// </summary>
public abstract class ListUsersEndpointBase : EndpointWithoutRequest<PaginatedResponse<UserResponse>>
{
    /// <summary>Initializes a new instance of the <see cref="ListUsersEndpointBase"/> class.</summary>
        private readonly UserConfigurationProvider _userProvider;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="ListUsersEndpointBase"/> class.</summary>
    protected ListUsersEndpointBase(ILogger logger, UserConfigurationProvider userProvider)
    {
        EndpointLogger = logger;
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
        Get("/users");
        Policies(ReadPolicy);
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (auth, summary, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        
        var result = await _userProvider.GetAllUsers(ct).ConfigureAwait(false);

        // Why this refuses instead of answering: "the user store could not be read" and "this
        // deployment has no users" are opposite facts, and a 200 carrying an empty page says the
        // second when only the first is true. A caller cannot tell them apart, so the screen shows
        // an empty Users tab and nobody goes looking for the failure.
        if (!result.IsSuccess || result.Value is null)
        {
            UserEndpointLog.UserStoreUnreadable(EndpointLogger, result.CurrentMessage ?? "no value returned");
            await Send.ResponseAsync(
                PaginatedResponse<UserResponse>.Create([], 0, 0, 0),
                StatusCodes.Status500InternalServerError, ct).ConfigureAwait(false);
            return;
        }

        var users = result.Value.Select(MapToResponse).ToList();
        await Send.OkAsync(PaginatedResponse<UserResponse>.Create(users, 0, users.Count, users.Count), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps a user entity to a response DTO. Override for custom mapping.
    /// </summary>
    protected abstract UserResponse MapToResponse(IUser user);
}
