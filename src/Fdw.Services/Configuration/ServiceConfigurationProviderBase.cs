using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Configuration;

/// <summary>
/// A domain's configuration provider: it reads the domain's own rows to learn which implementation a
/// member is, and hands the request to that implementation's provider.
/// </summary>
/// <typeparam name="TDomainConfiguration">The domain record.</typeparam>
/// <typeparam name="TImplementationConfiguration">The domain's implementation configuration contract.</typeparam>
/// <typeparam name="TCommand">The domain record's configuration command.</typeparam>
/// <remarks>
/// It owns the dictionary of implementation providers, keyed by <c>ServiceOptionType</c>, and it is the
/// only thing holding a gateway. An implementation provider receives the gateway as an argument, so it
/// reads and writes in the same connection as the domain it belongs to — which the foreign key already
/// required, being declared on the domain row's <c>RowId</c>.
/// </remarks>
public abstract class ServiceConfigurationProviderBase<TDomainConfiguration, TImplementationConfiguration, TCommand>
    : ImplementationConfigurationProviderBase<TDomainConfiguration, TCommand>,
      IDomainConfigurationProvider<TImplementationConfiguration>
    where TDomainConfiguration : class, IGenericConfiguration
    where TImplementationConfiguration : IImplementationConfiguration
    where TCommand : ConfigurationCommandBase<TDomainConfiguration>
{
    private readonly ILogger _log;

    /// <summary>The implementation providers this domain dispatches to, keyed by ServiceOptionType.</summary>
    /// <remarks>
    /// Kept alongside the erased registry the compose step uses, because dispatch has to call
    /// <see cref="IImplementationConfigurationProvider{T}.Get(Guid, CancellationToken)"/> and get
    /// <typeparamref name="TImplementationConfiguration"/> back. The erased entry cannot return it.
    /// </remarks>
    private readonly ConcurrentDictionary<string, IImplementationConfigurationProvider<TImplementationConfiguration>> _implementations
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ServiceConfigurationProviderBase{TDomainConfiguration, TImplementationConfiguration, TCommand}"/> class.
    /// </summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the named connection.</param>
    /// <param name="dataStoreName">The store the domain's rows live in.</param>
    /// <param name="pathName">The schema the domain's rows live in.</param>
    protected ServiceConfigurationProviderBase(
        ILogger<ImplementationConfigurationProviderBase<TDomainConfiguration, TCommand>>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName)
        : base(logger, gatewayProvider, dataStoreName, pathName)
        => _log = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

    /// <inheritdoc />
    public IGenericResult Register<T>(string name, T implementationConfigurationProvider)
        where T : IImplementationConfigurationProvider<TImplementationConfiguration>
    {
        if (implementationConfigurationProvider is not IServiceConfigurationProvider erased)
        {
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.ProviderNotErasable(
                    _log, name, implementationConfigurationProvider?.GetType().FullName ?? "(null)"));
        }

        base.Register(name, erased);
        _implementations[name] = implementationConfigurationProvider;
        return GenericResult.Success();
    }

    /// <inheritdoc />
    async Task<IGenericResult<TImplementationConfiguration>> IDomainConfigurationProvider<TImplementationConfiguration>.Get(
        string name, CancellationToken cancellationToken)
        => await Dispatch(await Get(name, cancellationToken).ConfigureAwait(false), name, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    async Task<IGenericResult<TImplementationConfiguration>> IDomainConfigurationProvider<TImplementationConfiguration>.Get(
        Guid id, CancellationToken cancellationToken)
        => await Dispatch(await Get(id, cancellationToken).ConfigureAwait(false), id.ToString(), cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    async Task<IGenericResult> IDomainConfigurationProvider<TImplementationConfiguration>.Save<T>(
        string serviceOptionType, string name, T implementationConfiguration, CancellationToken cancellationToken)
    {
        if (!ImplementationProviders.TryGetValue(serviceOptionType, out _))
        {
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.NoImplementationProvider(_log, name, serviceOptionType));
        }

        return await Save(Compose(serviceOptionType, name, implementationConfiguration), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Builds the domain record that carries a member's name, kind and implementation.</summary>
    /// <typeparam name="T">The implementation configuration being written.</typeparam>
    /// <param name="serviceOptionType">Which implementation this member is.</param>
    /// <param name="name">The member's name.</param>
    /// <param name="implementationConfiguration">The implementation's own configuration.</param>
    /// <returns>The domain record to save.</returns>
    protected abstract TDomainConfiguration Compose<T>(
        string serviceOptionType, string name, T implementationConfiguration)
        where T : TImplementationConfiguration;

    /// <inheritdoc />
    Task<IGenericResult> IDomainConfigurationProvider<TImplementationConfiguration>.Delete(
        Guid id, CancellationToken cancellationToken) => Delete(id, cancellationToken);

    /// <inheritdoc />
    Task<IGenericResult> IDomainConfigurationProvider<TImplementationConfiguration>.Delete(
        string name, CancellationToken cancellationToken) => Delete(name, cancellationToken);

    /// <summary>Hands a domain record to the provider for the implementation it names.</summary>
    /// <remarks>
    /// The domain record says which implementation a member is; this looks that name up in the
    /// registry and asks that provider for the member's own configuration. A name with no registered
    /// provider is a failed result — there is no hook for a domain to answer it differently.
    /// </remarks>
    private async Task<IGenericResult<TImplementationConfiguration>> Dispatch(
        IGenericResult<TDomainConfiguration> domainRecord,
        string identifier,
        CancellationToken cancellationToken)
    {
        if (!domainRecord.IsSuccess || domainRecord.Value is null)
            return domainRecord.ToNewResult<TImplementationConfiguration>();

        var serviceOptionType = domainRecord.Value.ServiceOptionType;
        if (string.IsNullOrWhiteSpace(serviceOptionType))
            return GenericResult<TImplementationConfiguration>.Failure(
                DefaultConfigurationProviderLog.RecordHasNoServiceOptionType(_log, identifier));

        return _implementations.TryGetValue(serviceOptionType, out var implementation)
            ? await implementation.Get(domainRecord.Value.Id, cancellationToken).ConfigureAwait(false)
            : GenericResult<TImplementationConfiguration>.Failure(
                DefaultConfigurationProviderLog.NoImplementationProvider(_log, identifier, serviceOptionType));
    }
}
