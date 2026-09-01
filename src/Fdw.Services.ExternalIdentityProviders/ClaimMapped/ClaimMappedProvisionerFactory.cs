using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
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
    : IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>
{
    private readonly UserConfigurationProvider _users;
    private readonly UserRoleConfigurationProvider _userRoles;
    private readonly RoleConfigurationProvider _roles;
    private readonly ImplementationConfigurationProviderBase<ExternalIdentityConfiguration, ExternalIdentityConfigurationCommand> _identities;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ClaimMappedProvisionerFactory> _logger;

    /// <summary>Initializes a new instance of the <see cref="ClaimMappedProvisionerFactory"/> class.</summary>
    public ClaimMappedProvisionerFactory(
        UserConfigurationProvider users,
        UserRoleConfigurationProvider userRoles,
        RoleConfigurationProvider roles,
        ImplementationConfigurationProviderBase<ExternalIdentityConfiguration, ExternalIdentityConfigurationCommand> identities,
        ILoggerFactory? loggerFactory)
    {
        _users = users;
        _userRoles = userRoles;
        _roles = roles;
        _identities = identities;
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
            typed, _users, _userRoles, _roles, _identities,
            _loggerFactory.CreateLogger<ClaimMappedProvisioner>());

        return GenericResult<IExternalIdentityProvisioner>.Success(provisioner);
    }

    /// <inheritdoc />
    public IGenericResult<IExternalIdentityProvisioner> Create(
        IExternalIdentityProvisionerImplementationConfiguration configuration,
        IPlatformServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration> provisionerProvider)
        // No siblings to delegate to, so the provider argument is unused — the overload exists only
        // to satisfy the interface every provisioner factory implements.
        => Create(configuration);

    /// <inheritdoc />
    public IGenericResult<IExternalIdentityProvisioner> Create(IGenericConfiguration configuration)
    {
        if (configuration is IExternalIdentityProvisionerImplementationConfiguration typed)
            return Create(typed);

        return GenericResult<IExternalIdentityProvisioner>.Failure(
            ExternalIdentityProvisionerLog.FactoryCreateFailed(_logger, configuration?.Name ?? "(null)",
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
            ExternalIdentityProvisionerLog.FactoryCreateFailed(_logger, configuration?.Name ?? "(null)",
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
