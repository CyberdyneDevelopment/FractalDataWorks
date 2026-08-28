using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authorization.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Generic base endpoint for creating a new role.
/// </summary>
public abstract class CreateRoleEndpointBase : Endpoint<CreateRoleRequest, RoleSummaryResponse>
{
    private readonly RoleConfigurationProvider _roleProvider;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    protected CreateRoleEndpointBase(RoleConfigurationProvider roleProvider)
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
    protected virtual string WritePolicy => "settings/role:write";

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/roles");
        Policies(WritePolicy);
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (auth, summary, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(CreateRoleRequest req, CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        OnCreatingRole(req.Name);

        Guid? parentRoleId = null;
        if (!string.IsNullOrEmpty(req.ParentRoleName))
        {
            var parent = await _roleProvider.GetRole(req.ParentRoleName, ct).ConfigureAwait(false);
            if (parent is null)
            {
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

            parentRoleId = parent.Id;
        }

        var config = BuildConfiguration(req, parentRoleId);

        var result = await _roleProvider.Save(config, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            await Send.ResponseAsync(new RoleSummaryResponse { Name = req.Name }, 400, ct).ConfigureAwait(false);
            return;
        }

        var saved = result.Value!;
        OnRoleCreated(saved.Name, saved.Id);

        await Send.ResponseAsync(MapToSummary(saved), 201, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the RoleConfiguration from the create request.
    /// Override to customize configuration creation.
    /// </summary>
    protected virtual RoleConfiguration BuildConfiguration(CreateRoleRequest request, Guid? parentRoleId)
    {
        return new RoleConfiguration
        {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            IsTenantScoped = request.IsTenantScoped,
            ParentRoleId = parentRoleId
        };
    }

    /// <summary>
    /// Maps a RoleConfiguration to a summary DTO.
    /// Override to customize the mapping.
    /// </summary>
    protected virtual RoleSummaryResponse MapToSummary(RoleConfiguration role)
    {
        return new RoleSummaryResponse
        {
            Id = role.Id,
            Name = role.Name,
            DisplayName = role.DisplayName,
            Description = role.Description,
            IsTenantScoped = role.IsTenantScoped,
            SortOrder = role.SortOrder,
            CreatedAt = role.CreateDate
        };
    }

    /// <summary>
    /// Called when creating a role. Override for custom logging.
    /// </summary>
    protected virtual void OnCreatingRole(string roleName)
    {
    }

    /// <summary>
    /// Called when a role has been created. Override for custom logging.
    /// </summary>
    protected virtual void OnRoleCreated(string roleName, Guid roleId)
    {
    }
}
