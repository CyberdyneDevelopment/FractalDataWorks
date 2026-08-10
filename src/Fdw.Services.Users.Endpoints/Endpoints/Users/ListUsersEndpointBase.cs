using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Models;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Generic base endpoint for listing all users.
/// </summary>
public abstract class ListUsersEndpointBase : EndpointWithoutRequest<PaginatedResponse<UserResponse>>
{
    private readonly UserConfigurationProvider _userProvider;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    protected ListUsersEndpointBase(UserConfigurationProvider userProvider)
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
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        var result = await _userProvider.GetAllUsers(ct).ConfigureAwait(false);

        // Why: Newman/clients expect a paginated envelope {items, skip, take, totalCount, hasMore}
        // matching the response shape from /pipelines and other Crud-list endpoints.
        if (!result.IsSuccess || result.Value is null)
        {
            await Send.OkAsync(PaginatedResponse<UserResponse>.Create([], 0, 0, 0), ct).ConfigureAwait(false);
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
