using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Fdw.Services.Authorization.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Generic base endpoint for updating a role.
/// </summary>
public abstract class UpdateRoleEndpointBase : Endpoint<UpdateRoleRequest>
{
    // The domain and implementation a role row is written under. These are values the seed already
    // writes (authz.Role Domain/Implementation = 'Role'), not names this endpoint gets to choose:
    // a save under any other pair produces a row nothing composes on the next read.
    private const string RoleDomain = "Role";
    private const string RoleImplementation = "Role";

    /// <summary>Initializes a new instance of the <see cref="UpdateRoleEndpointBase"/> class.</summary>
        private readonly IRoleConfigurationProvider _roleProvider;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="UpdateRoleEndpointBase"/> class.</summary>
    protected UpdateRoleEndpointBase(ILogger logger, IRoleConfigurationProvider roleProvider)
    {
        EndpointLogger = logger;
        _roleProvider = roleProvider;
    }


    /// <summary>
    /// Gets the role configuration provider.
    /// </summary>
    protected IRoleConfigurationProvider RoleProvider => _roleProvider;

    /// <summary>
    /// Gets the RBAC policy required by this endpoint. Defaults to "settings/role:write".
    /// </summary>
    protected virtual string WritePolicy => "settings/role:write";

    /// <inheritdoc />
    public override void Configure()
    {
        Patch("/roles/{Name}");
        Policies(WritePolicy);
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (auth, summary, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(UpdateRoleRequest req, CancellationToken ct)
    {
        
        var existing = await _roleProvider.Get(req.Name, ct).ConfigureAwait(false);

        // A failed read is not an absent role -- 404 here would tell the caller the role is gone.
        if (!existing.IsSuccess)
        {
            await Send.ResponseAsync(null, 500, ct).ConfigureAwait(false);
            return;
        }

        if (existing.Value is null)
        {
            HttpContext.Response.StatusCode = 404;
            HttpContext.Response.ContentType = "application/json";
            await HttpContext.Response.WriteAsJsonAsync(new { errorCode = "NotFound", messages = new[] { $"roles \u0027{req.Name}\u0027 was not found." } }, ct).ConfigureAwait(false);
            return;
        }

        var updated = ApplyUpdates(existing.Value, req);

        var result = await _roleProvider.Save(updated, RoleDomain, RoleImplementation, updated.Name, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            await Send.ResponseAsync(null, 400, ct).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies updates from the request to the existing configuration.
    /// Override to customize update logic.
    /// </summary>
    protected virtual IRoleImplementationConfiguration ApplyUpdates(IRoleImplementationConfiguration existing, UpdateRoleRequest request)
    {
        if (request.DisplayName is not null)
        {
            existing.DisplayName = request.DisplayName;
        }

        if (request.Description is not null)
        {
            existing.Description = request.Description;
        }

        if (request.SortOrder.HasValue)
        {
            existing.SortOrder = request.SortOrder.Value;
        }

        return existing;
    }
}
