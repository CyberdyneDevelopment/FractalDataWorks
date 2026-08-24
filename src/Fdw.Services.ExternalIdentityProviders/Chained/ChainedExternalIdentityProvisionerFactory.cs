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
/// <see cref="ExternalIdentityProvisionerConfiguration"/> header (whose <c>Configuration</c> property
/// carries the composed <see cref="ChainedExternalIdentityProvisionerConfiguration"/> typed body).
/// A PURE constructor: it holds no providers and resolves nothing. The provisioner provider needed for
/// Provision-time sibling lookup is supplied by the provider itself, as an argument to
/// <see cref="Create(ExternalIdentityProvisionerConfiguration, IPlatformServiceProvider{IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration})"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Do not ctor-inject a provider here.</strong> This factory is resolved from inside the
/// source-generated scoped resolver lambda for
/// <c>IPlatformServiceProvider&lt;IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration&gt;</c>.
/// Taking that provider as a constructor dependency made resolving the factory re-enter the same lambda —
/// whose cache entry is not published yet — producing unbounded recursion. MEDI's StackGuard migrates it
/// onto fresh stacks instead of throwing, so the host HANGS SILENTLY (no exception, no log) until it is
/// killed (FDW-615). Keeping the factory pure and letting the provider pass <c>this</c> removes the
/// container from the path entirely. Mirrors <c>IDataVaultFactory.Create(config, connection, pepper)</c>.
/// </para>
/// </remarks>
internal sealed class ChainedExternalIdentityProvisionerFactory
    : IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>
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
    // Why: the parameterless-provider overload cannot build a working Chained provisioner — a chain
    // MUST be able to resolve its steps' sibling provisioners at Provision time. Fail loud rather than
    // hand back a provisioner with a null provider that would NRE at the first Provision call.
    public IGenericResult<IExternalIdentityProvisioner> Create(ExternalIdentityProvisionerConfiguration configuration)
        => GenericResult<IExternalIdentityProvisioner>.Failure(
            ExternalIdentityProvisionerLog.FactoryCreateFailed(_logger, configuration?.Name ?? "(null)",
                "a Chained provisioner requires the provisioner provider for sibling lookup; it must be created "
                + "via Create(configuration, provisionerProvider) — the provider supplies itself. "
                + "Reaching this overload means the provisioner domain is not using DefaultExternalIdentityProvisionerProvider."));

    /// <inheritdoc />
    public IGenericResult<IExternalIdentityProvisioner> Create(
        ExternalIdentityProvisionerConfiguration configuration,
        IPlatformServiceProvider<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration> provisionerProvider)
    {
        if (configuration is null)
            return GenericResult<IExternalIdentityProvisioner>.Failure(
                ExternalIdentityProvisionerLog.FactoryCreateFailed(_logger, "(null)", "configuration was null."));

        if (provisionerProvider is null)
            return GenericResult<IExternalIdentityProvisioner>.Failure(
                ExternalIdentityProvisionerLog.FactoryCreateFailed(_logger, configuration.Name, "provisionerProvider was null."));

        if (configuration.Configuration is not ChainedExternalIdentityProvisionerConfiguration typed)
            return GenericResult<IExternalIdentityProvisioner>.Failure(
                ExternalIdentityProvisionerLog.FactoryCreateFailed(_logger, configuration.Name,
                    "no composed ChainedExternalIdentityProvisionerConfiguration typed body — the header's ServiceOptionType must be 'Chained' and the typed provider must be registered."));

        var provisioner = new ChainedExternalIdentityProvisioner(
            configuration, typed, provisionerProvider, _loggerFactory.CreateLogger<ChainedExternalIdentityProvisioner>());

        return GenericResult<IExternalIdentityProvisioner>.Success(provisioner);
    }

    /// <inheritdoc />
    public IGenericResult<IExternalIdentityProvisioner> Create(IGenericConfiguration configuration)
    {
        if (configuration is ExternalIdentityProvisionerConfiguration typed)
            return Create(typed);

        return GenericResult<IExternalIdentityProvisioner>.Failure(
            ExternalIdentityProvisionerLog.FactoryCreateFailed(_logger, configuration?.Name ?? "(null)",
                $"expected ExternalIdentityProvisionerConfiguration but received '{configuration?.GetType().FullName ?? "null"}'."));
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
