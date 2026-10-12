using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Fdw.Services.Authorization.Configuration;
using Microsoft.Extensions.Logging;
using Fdw.Services.TokenManagers.Abstractions;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Generic base endpoint for deleting a role.
/// </summary>
public abstract class DeleteRoleEndpointBase : Endpoint<GetRoleRequest>
{
    /// <summary>Initializes a new instance of the <see cref="DeleteRoleEndpointBase"/> class.</summary>
        private readonly IRoleConfigurationProvider _roleProvider;
    private readonly IUserRoleConfigurationProvider _userRoles;
    private readonly ITokenRevocationStore _revocations;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="DeleteRoleEndpointBase"/> class.</summary>
    protected DeleteRoleEndpointBase(
        ILogger logger,
        IRoleConfigurationProvider roleProvider,
        IUserRoleConfigurationProvider userRoles,
        ITokenRevocationStore revocations)
    {
        EndpointLogger = logger;
        _roleProvider = roleProvider;
        _userRoles = userRoles;
        _revocations = revocations;
    }


    /// <summary>
    /// Gets the role configuration provider.
    /// </summary>
    protected IRoleConfigurationProvider RoleProvider => _roleProvider;

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
        AuthorizationEndpointLog.DeletingRole(EndpointLogger, req.Name);

        var existing = await _roleProvider.Get(req.Name, ct).ConfigureAwait(false);

        // A read that FAILED and a role that is genuinely absent are opposite facts; reporting the
        // first as 404 tells an administrator the role is gone when the store could not answer.
        if (!existing.IsSuccess)
        {
            AuthorizationEndpointLog.AuthorizationReadFailed(EndpointLogger, req.Name, existing.CurrentMessage);
            ThrowError(existing.CurrentMessage ?? "Failed to read role", 500);
            return;
        }

        if (existing.Value is null)
        {
            AuthorizationEndpointLog.RoleNotFound(EndpointLogger, req.Name);
            await Send.ResultAsync(TypedResults.Json(new { errorCode = "NotFound", messages = new[] { $"roles \u0027{req.Name}\u0027 was not found." } }, statusCode: 404)).ConfigureAwait(false);
            return;
        }

        AuthorizationEndpointLog.RoleResolved(EndpointLogger, req.Name, existing.Value.Id);

        OnDeletingRole(req.Name, existing.Value.Id);

        var result = await _roleProvider.Delete(existing.Value.Id, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            AuthorizationEndpointLog.RoleWriteFailed(EndpointLogger, "delete", req.Name, result.CurrentMessage);
            ThrowError(result.CurrentMessage ?? "Failed to delete role", 500);
            return;
        }

        AuthorizationEndpointLog.RoleDeleted(EndpointLogger, req.Name);

        // The role's grants are gone, so no holder may keep a token that still carries them (FDW-875).
        var ended = await RoleHolderSessions.End(_userRoles, _revocations, existing.Value.Id, req.Name, EndpointLogger, ct).ConfigureAwait(false);
        if (ended.IsFailure)
        {
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
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
