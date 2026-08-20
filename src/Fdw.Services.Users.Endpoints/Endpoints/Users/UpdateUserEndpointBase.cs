using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Users;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Generic base endpoint for updating a user.
/// </summary>
/// <typeparam name="TRequest">The request type, host-extensible beyond <see cref="UpdateUserRequest"/>.</typeparam>
public abstract class UpdateUserEndpointBase<TRequest> : Endpoint<TRequest>
    where TRequest : UpdateUserRequest
{
    private readonly UserConfigurationProvider _userProvider;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    protected UpdateUserEndpointBase(UserConfigurationProvider userProvider)
    {
        _userProvider = userProvider;
    }

    /// <summary>
    /// Gets the user provider.
    /// </summary>
    protected UserConfigurationProvider UserProvider => _userProvider;

    /// <summary>
    /// Gets the RBAC policy required by this endpoint. Defaults to "users:write".
    /// </summary>
    // Why: the standard CRUD tier for this resource. This endpoint previously required ":delete"
    // as an ad-hoc "Admin-only" tier, because the seeded Operator role is granted ":write" on
    // every resource by a blanket rule and would otherwise have inherited user administration.
    // The grant was the wrong thing to work around: user/role admin is now carved out of
    // Operator in the seed, so these permissions can mean exactly what they say (FDW-634).
    protected virtual string WritePolicy => "users:write";

    /// <inheritdoc />
    public override void Configure()
    {
        // Why: callers identify users by name in the URL; binding {Name} as string avoids the
        // Guid binder rejecting "/users/admin" with HTTP 400 before any auth/handler runs.
        Patch("/users/{Name}");
        Policies(WritePolicy);
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (auth, summary, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(TRequest req, CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        var result = await Update(req, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            ThrowError(result.CurrentMessage ?? "Failed to update user", 500);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the update operation. Implementers must provide update logic.
    /// </summary>
    protected abstract Task<IGenericResult> Update(TRequest request, CancellationToken ct);
}
