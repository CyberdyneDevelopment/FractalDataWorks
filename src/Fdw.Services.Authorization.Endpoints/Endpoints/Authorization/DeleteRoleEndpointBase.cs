using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Fdw.Services.Authorization.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Generic base endpoint for deleting a role.
/// </summary>
public abstract class DeleteRoleEndpointBase : Endpoint<GetRoleRequest>
{
    private readonly RoleConfigurationProvider _roleProvider;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    protected DeleteRoleEndpointBase(RoleConfigurationProvider roleProvider)
    {
        _roleProvider = roleProvider;
    }

    /// <summary>
    /// Gets the role configuration provider.
    /// </summary>
    protected RoleConfigurationProvider RoleProvider => _roleProvider;

    /// <summary>
    /// Gets the RBAC policy required by this endpoint. Defaults to "settings/role:delete".
    /// </summary>
    protected virtual string DeletePolicy => "settings/role:delete";

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/roles/{Name}");
        Policies(DeletePolicy);
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (auth, summary, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(GetRoleRequest req, CancellationToken ct)
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

        OnDeletingRole(req.Name, existing.Id);

        var result = await _roleProvider.Delete(existing.Id, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            ThrowError(result.CurrentMessage ?? "Failed to delete role", 500);
            return;
        }

        OnRoleDeleted(req.Name);

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Called when deleting a role. Override for custom logging.
    /// </summary>
    protected virtual void OnDeletingRole(string roleName, Guid roleId)
    {
    }

    /// <summary>
    /// Called when a role has been deleted. Override for custom logging.
    /// </summary>
    protected virtual void OnRoleDeleted(string roleName)
    {
    }
}
