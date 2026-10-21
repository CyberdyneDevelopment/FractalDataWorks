using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authorization.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Generic base endpoint for creating a new role.
/// </summary>
public abstract class CreateRoleEndpointBase : Endpoint<CreateRoleRequest, RoleSummaryResponse>
{
    // The domain and implementation a role row is written under. These are values the seed already
    // writes (authz.Role Domain/Implementation = 'Role'), not names this endpoint gets to choose:
    // a save under any other pair produces a row nothing composes on the next read.
    private const string RoleDomain = "Role";
    private const string RoleImplementation = "Role";

    /// <summary>Initializes a new instance of the <see cref="CreateRoleEndpointBase"/> class.</summary>
        private readonly IRoleConfigurationProvider _roleProvider;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="CreateRoleEndpointBase"/> class.</summary>
    protected CreateRoleEndpointBase(ILogger logger, IRoleConfigurationProvider roleProvider)
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
        
        OnCreatingRole(req.Name);

        var config = BuildConfiguration(req);

        var result = await _roleProvider.Save(config, RoleDomain, RoleImplementation, req.Name, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            await Send.ResponseAsync(new RoleSummaryResponse { Name = req.Name }, 400, ct).ConfigureAwait(false);
            return;
        }

        // Save stamps the record it was handed -- name, domain, implementation and the domain row's
        // durable Id -- and returns no value of its own, so the record IS what was written.
        var audited = await RoleChangeAudit.RecordRole(Resolve<IAdministrativeAudit>(), User, "RoleCreated", req.Name,
            null, RoleChangeAudit.Describe(config.DisplayName, config.Description), ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            AuthorizationEndpointLog.RoleDefinitionNotAudited(EndpointLogger, "RoleCreated", req.Name, audited.CurrentMessage);
            await Send.ResponseAsync(new RoleSummaryResponse { Name = req.Name }, 500, ct).ConfigureAwait(false);
            return;
        }

        OnRoleCreated(config.Name, config.Id);

        await Send.ResponseAsync(MapToSummary(config), 201, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the RoleImplementationConfiguration from the create request.
    /// Override to customize configuration creation.
    /// </summary>
    protected virtual RoleImplementationConfiguration BuildConfiguration(CreateRoleRequest request)
    {
        return new RoleImplementationConfiguration
        {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            IsTenantScoped = request.IsTenantScoped,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>
    /// Maps a RoleImplementationConfiguration to a summary DTO.
    /// Override to customize the mapping.
    /// </summary>
    protected virtual RoleSummaryResponse MapToSummary(RoleImplementationConfiguration role)
    {
        return new RoleSummaryResponse
        {
            Id = role.Id,
            Name = role.Name,
            DisplayName = role.DisplayName,
            Description = role.Description,
            IsTenantScoped = role.IsTenantScoped,
            CreatedAt = role.CreatedAt
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
