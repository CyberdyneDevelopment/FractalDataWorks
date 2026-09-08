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
/// <typeparam name="TDomainContract">
/// The domain's configuration contract — the interface consumers name, which
/// <typeparamref name="TDomainConfiguration"/> implements. It exists because the abstractions package
/// cannot see the concrete record and the concrete record is what this provider reads.
/// </typeparam>
/// <typeparam name="TImplementationConfiguration">The domain's implementation configuration contract.</typeparam>
/// <typeparam name="TCommand">The domain record's configuration command.</typeparam>
/// <remarks>
/// It owns the dictionary of implementation providers, keyed by <c>ServiceOptionType</c>, and it is the
/// only thing holding a gateway. An implementation provider receives the gateway as an argument, so it
/// reads and writes in the same connection as the domain it belongs to — which the foreign key already
/// required, being declared on the domain row's <c>RowId</c>.
/// </remarks>
public abstract class ServiceConfigurationProviderBase<TDomainConfiguration, TDomainContract, TImplementationConfiguration, TCommand>
    : ImplementationConfigurationProviderBase<TDomainConfiguration, TCommand>,
      IDomainConfigurationProvider<TDomainContract, TImplementationConfiguration>
    where TDomainConfiguration : class, TDomainContract
    where TDomainContract : class, IDomainConfiguration
    where TImplementationConfiguration : IImplementationConfiguration
    where TCommand : ConfigurationCommandBase<TDomainConfiguration>
{
    private readonly ILogger _log;


    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ServiceConfigurationProviderBase{TDomainConfiguration, TDomainContract, TImplementationConfiguration, TCommand}"/> class.
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
        return GenericResult.Success();
    }

    /// <inheritdoc />
    async Task<IGenericResult<TDomainContract>> IDomainConfigurationProvider<TDomainContract, TImplementationConfiguration>.Get(
        string name, CancellationToken cancellationToken)
    {
        // ToNewResult throws on a SUCCESSFUL result, so the success path widens explicitly.
        var record = await Get(name, cancellationToken).ConfigureAwait(false);
        return record.IsSuccess && record.Value is { } value
            ? GenericResult<TDomainContract>.Success(value)
            : record.ToNewResult<TDomainContract>();
    }

    /// <inheritdoc />
    async Task<IGenericResult<TDomainContract>> IDomainConfigurationProvider<TDomainContract, TImplementationConfiguration>.Get(
        Guid id, CancellationToken cancellationToken)
    {
        // ToNewResult throws on a SUCCESSFUL result, so the success path widens explicitly.
        var record = await Get(id, cancellationToken).ConfigureAwait(false);
        return record.IsSuccess && record.Value is { } value
            ? GenericResult<TDomainContract>.Success(value)
            : record.ToNewResult<TDomainContract>();
    }

    /// <inheritdoc />
    async Task<IGenericResult> IDomainConfigurationProvider<TDomainContract, TImplementationConfiguration>.Save<T>(
        string serviceOptionType, string name, T implementationConfiguration, CancellationToken cancellationToken)
    {
        if (!ImplementationProviders.TryGetValue(serviceOptionType, out _))
        {
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.NoImplementationProvider(_log, name, serviceOptionType));
        }

        // The interface cannot name this domain's implementation contract, so the caller's value is
        // widened to IImplementationConfiguration there and narrowed back here. A mismatch is a
        // caller handing one domain's implementation to another's provider — fail rather than throw.
        if (implementationConfiguration is not TImplementationConfiguration typed)
        {
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.ProviderNotErasable(
                    _log, name, implementationConfiguration?.GetType().FullName ?? "(null)"));
        }

        return await Save(Compose(serviceOptionType, name, typed), cancellationToken)
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
    Task<IGenericResult> IDomainConfigurationProvider<TDomainContract, TImplementationConfiguration>.Delete(
        Guid id, CancellationToken cancellationToken) => Delete(id, cancellationToken);

    /// <inheritdoc />
    Task<IGenericResult> IDomainConfigurationProvider<TDomainContract, TImplementationConfiguration>.Delete(
        string name, CancellationToken cancellationToken) => Delete(name, cancellationToken);

}
