using System;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Chained;

/// <summary>
/// Factory that builds <see cref="ChainedExternalIdentityProvisioner"/> instances from a resolved
/// <see cref="IExternalIdentityProvisionerImplementationConfiguration"/> header (whose <c>Configuration</c> property
/// carries the composed <see cref="ChainedExternalIdentityProvisionerConfiguration"/> typed body).
/// A PURE constructor: it holds no providers and resolves nothing. The provisioner provider needed for
/// Provision-time sibling lookup is supplied by the provider itself, as an argument to
/// its <c>Create</c> overload that takes the provider.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Do not ctor-inject a provider here.</strong> This factory is resolved from inside the
/// source-generated scoped resolver lambda for
/// <c>IPlatformServiceProvider&lt;IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration&gt;</c>.
/// Taking that provider as a constructor dependency made resolving the factory re-enter the same lambda —
/// whose cache entry is not published yet — producing unbounded recursion. MEDI's StackGuard migrates it
/// onto fresh stacks instead of throwing, so the host HANGS SILENTLY (no exception, no log) until it is
/// killed (FDW-615). Keeping the factory pure and letting the provider pass <c>this</c> removes the
/// container from the path entirely. Mirrors <c>IDataVaultFactory.Create(config, connection, pepper)</c>.
/// </para>
/// </remarks>
internal sealed class ChainedExternalIdentityProvisionerFactory
    : IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ChainedExternalIdentityProvisionerFactory> _logger;

    /// <summary>Initializes a new instance of the <see cref="ChainedExternalIdentityProvisionerFactory"/> class.</summary>
    public ChainedExternalIdentityProvisionerFactory(ILoggerFactory? loggerFactory)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<ChainedExternalIdentityProvisionerFactory>();
    }

    /// <inheritdoc />
    public IGenericResult<IExternalIdentityProvisioner> Create(IExternalIdentityProvisionerImplementationConfiguration configuration)
        => GenericResult<IExternalIdentityProvisioner>.Failure(
            ExternalIdentityProvisionerLog.FactoryCreateFailed(_logger, configuration?.Name ?? "(null)",
                "a Chained provisioner requires the provisioner provider for sibling lookup; it must be created "
                + "via Create(configuration, provisionerProvider) — the provider supplies itself. "
                + "Reaching this overload means the provisioner domain is not using DefaultExternalIdentityProvisionerProvider."));

    /// <inheritdoc />
    public IGenericResult<IExternalIdentityProvisioner> Create(
        IExternalIdentityProvisionerImplementationConfiguration configuration,
        IPlatformServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration> provisionerProvider)
    {
        if (configuration is null)
            return GenericResult<IExternalIdentityProvisioner>.Failure(
                ExternalIdentityProvisionerLog.FactoryCreateFailed(_logger, "(null)", "configuration was null."));

        if (provisionerProvider is null)
            return GenericResult<IExternalIdentityProvisioner>.Failure(
                ExternalIdentityProvisionerLog.FactoryCreateFailed(_logger, configuration.Name, "provisionerProvider was null."));

        if (configuration is not ChainedExternalIdentityProvisionerConfiguration typed)
            return GenericResult<IExternalIdentityProvisioner>.Failure(
                ExternalIdentityProvisionerLog.FactoryCreateFailed(_logger, configuration.Name,
                    "the configuration is not a ChainedExternalIdentityProvisionerConfiguration."));

        var provisioner = new ChainedExternalIdentityProvisioner(
            typed, provisionerProvider, _loggerFactory.CreateLogger<ChainedExternalIdentityProvisioner>());

        return GenericResult<IExternalIdentityProvisioner>.Success(provisioner);
    }

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
