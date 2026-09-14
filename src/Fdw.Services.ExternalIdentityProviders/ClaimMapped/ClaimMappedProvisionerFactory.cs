using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Fdw.Services.Configuration;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Fdw.Services.Users;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.ClaimMapped;

/// <summary>
/// Factory that builds <see cref="ClaimMappedProvisioner"/> instances from a resolved
/// <see cref="IExternalIdentityProvisionerImplementationConfiguration"/> header. Unlike
/// <see cref="Chained.ChainedExternalIdentityProvisionerFactory"/>, this one DOES take its
/// dependencies through the constructor — it never needs to resolve the provisioner provider itself
/// (it has no siblings to delegate to), so the re-entrancy hazard that forces Chained's factory to
/// stay pure does not apply here.
/// </summary>
internal sealed class ClaimMappedProvisionerFactory
    : IClaimMappedProvisionerFactory
{
    private readonly UserConfigurationProvider _users;
    private readonly UserRoleConfigurationProvider _userRoles;
    private readonly RoleConfigurationProvider _roles;
    private readonly IExternalIdentityConfigurationProvider _identities;
    private readonly UserTenantConfigurationProvider _userTenants;
    private readonly IAuthenticationContextAccessor? _authContextAccessor;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ClaimMappedProvisionerFactory> _logger;

    /// <summary>Initializes a new instance of the <see cref="ClaimMappedProvisionerFactory"/> class.</summary>
    /// <param name="users">Reads/writes the provisioned account.</param>
    /// <param name="userRoles">Grants the rule's mapped roles.</param>
    /// <param name="roles">Resolves a role name to its id.</param>
    /// <param name="identities">Links the provisioned account to the external subject.</param>
    /// <param name="userTenants">Grants tenant access when a matched rule opts in. Registered by the
    /// same <c>Fdw.Services.Users</c> option that already registers <paramref name="users"/>, so its
    /// availability is no wider a requirement than what this factory already depends on.</param>
    /// <param name="authContextAccessor">
    /// Elevates the write context for the (always pre-authentication) provisioning writes below, when
    /// available. Optional and defaulted, not required: only <c>MsSqlConnectionType</c> registers this
    /// accessor today, a domain <see cref="ClaimMappedProvisioner"/> has no existing dependency on —
    /// requiring it here would break any host that already uses ClaimMapped without also using MsSql
    /// connections. A host without it keeps today's un-elevated behavior unchanged.
    /// </param>
    /// <param name="loggerFactory">Builds this factory's and the provisioner's loggers.</param>
    public ClaimMappedProvisionerFactory(
        UserConfigurationProvider users,
        UserRoleConfigurationProvider userRoles,
        RoleConfigurationProvider roles,
        IExternalIdentityConfigurationProvider identities,
        UserTenantConfigurationProvider userTenants,
        IAuthenticationContextAccessor? authContextAccessor,
        ILoggerFactory? loggerFactory)
    {
        _users = users;
        _userRoles = userRoles;
        _roles = roles;
        _identities = identities;
        _userTenants = userTenants;
        _authContextAccessor = authContextAccessor;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<ClaimMappedProvisionerFactory>();
    }

    /// <inheritdoc />
    public IGenericResult<IExternalIdentityProvisioner> Create(IExternalIdentityProvisionerImplementationConfiguration configuration)
    {
        if (configuration is null)
            return GenericResult<IExternalIdentityProvisioner>.Failure(
                ExternalIdentityProvisionerLog.FactoryCreateFailed(_logger, "(null)", "configuration was null."));

        if (configuration is not ClaimMappedExternalIdentityProvisionerConfiguration typed)
            return GenericResult<IExternalIdentityProvisioner>.Failure(
                ExternalIdentityProvisionerLog.FactoryCreateFailed(_logger, configuration.Name,
                    "the configuration is not a ClaimMappedExternalIdentityProvisionerConfiguration."));

        var provisioner = new ClaimMappedProvisioner(
            typed, _users, _userRoles, _roles, _identities, _userTenants, _authContextAccessor,
            _loggerFactory.CreateLogger<ClaimMappedProvisioner>());

        return GenericResult<IExternalIdentityProvisioner>.Success(provisioner);
    }

    /// <inheritdoc />
    public IGenericResult<IExternalIdentityProvisioner> Create(
        IExternalIdentityProvisionerImplementationConfiguration configuration,
        IDomainServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration> provisionerProvider)
        // No siblings to delegate to, so the provider argument is unused — the overload exists only
        // to satisfy the interface every provisioner factory implements.
        => Create(configuration);

    /// <inheritdoc />
    public IGenericResult<IExternalIdentityProvisioner> Create(IGenericConfiguration configuration)
    {
        if (configuration is IExternalIdentityProvisionerImplementationConfiguration typed)
            return Create(typed);

        return GenericResult<IExternalIdentityProvisioner>.Failure(
            ExternalIdentityProvisionerLog.FactoryCreateFailed(_logger, configuration?.GetType().Name ?? "(null)",
                $"expected IExternalIdentityProvisionerImplementationConfiguration but received '{configuration?.GetType().FullName ?? "null"}'."));
    }

    /// <inheritdoc />
    public IGenericResult<T> Create<T>(IGenericConfiguration configuration) where T : IGenericService
    {
        var result = Create(configuration);
        if (!result.IsSuccess)
            return result.ToNewResult<T>();

        if (result.Value is T typed)
            return GenericResult<T>.Success(typed);

        return GenericResult<T>.Failure(
            ExternalIdentityProvisionerLog.FactoryCreateFailed(_logger, configuration?.GetType().Name ?? "(null)",
                $"created service does not implement requested type '{typeof(T).FullName}'."));
    }

    /// <inheritdoc />
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        var result = Create(configuration);
        return result.IsSuccess
            ? GenericResult<IGenericService>.Success(result.Value!)
            : result.ToNewResult<IGenericService>();
    }
}
