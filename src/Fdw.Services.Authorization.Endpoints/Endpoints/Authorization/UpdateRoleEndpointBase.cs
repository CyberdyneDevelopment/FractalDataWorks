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
    // Why: RoleConfigurationProvider replaces IOptionsMonitor<List<RoleConfiguration>> with dual-source
    // (ctrl + cfg) provider that merges system and user configurations.
    private readonly RoleConfigurationProvider _roleProvider;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    protected UpdateRoleEndpointBase(RoleConfigurationProvider roleProvider)
    {
        _roleProvider = roleProvider;
    }

    /// <summary>
    /// Gets the role configuration provider.
    /// </summary>
    protected RoleConfigurationProvider RoleProvider => _roleProvider;

    /// <summary>
    /// Gets the RBAC policy required by this endpoint. Defaults to "settings/role:write".
    /// </summary>
    // Why: the standard CRUD tier for this resource. This endpoint previously required ":delete"
    // as an ad-hoc "Admin-only" tier, because the seeded Operator role is granted ":write" on
    // every resource by a blanket rule and would otherwise have inherited user administration.
    // The grant was the wrong thing to work around: user/role admin is now carved out of
    // Operator in the seed, so these permissions can mean exactly what they say (FDW-634).
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
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        var existing = await _roleProvider.GetRole(req.Name, ct).ConfigureAwait(false);

        if (existing is null)
        {
            HttpContext.Response.StatusCode = 404;
            HttpContext.Response.ContentType = "application/json";
            await HttpContext.Response.WriteAsJsonAsync(new { errorCode = "NotFound", messages = new[] { $"roles \u0027{req.Name}\u0027 was not found." } }, ct).ConfigureAwait(false);
            return;
        }

        var updated = ApplyUpdates(existing, req);

        var result = await _roleProvider.Save(updated, ct).ConfigureAwait(false);
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
    protected virtual RoleConfiguration ApplyUpdates(RoleConfiguration existing, UpdateRoleRequest request)
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
