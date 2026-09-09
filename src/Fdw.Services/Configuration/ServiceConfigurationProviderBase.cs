using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration.Logging;
using Fdw.Data;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
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
    where TDomainConfiguration : class, IDomainConfiguration
    where TImplementationConfiguration : IImplementationConfiguration
    where TCommand : ConfigurationCommandBase<TDomainConfiguration>
{
    private readonly ILogger _log;


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

    // The registry of implementation providers, keyed by the discriminator a domain row carries.
    // It lives on the DOMAIN provider because only a domain has implementations to choose between --
    // an implementation provider has no such question to answer, which is why it no longer has this
    // dictionary and cannot be asked whether it is empty.
    private readonly ConcurrentDictionary<string, IServiceConfigurationProvider> _implementations
        = new(StringComparer.OrdinalIgnoreCase);

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

        _implementations[name] = erased;
        DefaultConfigurationProviderLog.TypedProviderRegistered(_log, typeof(TDomainConfiguration).Name, name);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    async Task<IGenericResult<IDomainConfiguration>> IDomainConfigurationProvider<TImplementationConfiguration>.Get(
        string name, CancellationToken cancellationToken)
    {
        var record = await Get(name, cancellationToken).ConfigureAwait(false);
        return record.IsSuccess && record.Value is { } domain
            ? GenericResult<IDomainConfiguration>.Success(domain)
            : record.ToNewResult<IDomainConfiguration>();
    }

    /// <inheritdoc />
    async Task<IGenericResult<IDomainConfiguration>> IDomainConfigurationProvider<TImplementationConfiguration>.Get(
        Guid id, CancellationToken cancellationToken)
    {
        var record = await Get(id, cancellationToken).ConfigureAwait(false);
        return record.IsSuccess && record.Value is { } domain
            ? GenericResult<IDomainConfiguration>.Success(domain)
            : record.ToNewResult<IDomainConfiguration>();
    }

    /// <inheritdoc />
    async Task<IGenericResult> IDomainConfigurationProvider<TImplementationConfiguration>.Save<T>(
        string implementation, string name, T implementationConfiguration, CancellationToken cancellationToken)
    {
        if (!_implementations.ContainsKey(implementation))
        {
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.NoImplementationProvider(_log, name, implementation));
        }

        return await Save(Compose(implementation, name, implementationConfiguration), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The domain half of composition: the row names its implementation, that name selects the
    /// implementation's own provider, and the result is attached. The base then composes the
    /// subtree. A row naming an implementation nobody registered is a fault and says so by name --
    /// there is no longer a case where "nothing to compose" and "nobody registered" look alike,
    /// because an implementation provider does not reach this code at all.
    /// </remarks>
    protected override async Task<IGenericResult<TDomainConfiguration>> ComposeAggregate(
        TDomainConfiguration header, DateTimeOffset? asOf, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(header.Implementation))
        {
            DefaultConfigurationProviderLog.NoServiceOptionTypeForTypedBody(
                _log, typeof(TDomainConfiguration).Name, header.Name);
        }
        else if (!_implementations.TryGetValue(header.Implementation, out var implementationProvider))
        {
            return GenericResult<TDomainConfiguration>.Failure(
                DefaultConfigurationProviderLog.NoImplementationProvider(
                    _log, header.Name, header.Implementation));
        }
        else
        {
            DefaultConfigurationProviderLog.LoadingTypedBody(
                _log, typeof(TDomainConfiguration).Name, header.Name, header.Implementation);

            var implementation = await implementationProvider.Get(header.Id, ct).ConfigureAwait(false);
            if (!implementation.IsSuccess)
            {
                return GenericResult<TDomainConfiguration>.Failure(
                    DefaultConfigurationProviderLog.TypedBodyLoadFailed(
                        _log, new InvalidOperationException(implementation.CurrentMessage),
                        typeof(TDomainConfiguration).Name, header.Name, header.Implementation));
            }

            // The implementation's name is the domain's. It is not a column on the implementation
            // row -- there is one name for a configured member, held where it is authored, and the
            // join that just produced this record is what carries it across. The registry is typed
            // to the erased provider, so the record arrives as IGenericConfiguration.
            if (implementation.Value is IImplementationConfiguration named)
            {
                named.Name = header.Name;
            }

            var mapper = PocoMapperCollection.ByName(typeof(TDomainConfiguration).Name);
            if (mapper == PocoMapperCollection.NotFound)
            {
                DefaultConfigurationProviderLog.NoMapperForTypedBody(
                    _log, typeof(TDomainConfiguration).Name, header.Name);
            }
            else
            {
                mapper.SetTypedBody(header, implementation.Value);
                DefaultConfigurationProviderLog.TypedBodyLoaded(
                    _log, typeof(TDomainConfiguration).Name, header.Name, header.Implementation);
            }
        }

        return await base.ComposeAggregate(header, asOf, ct).ConfigureAwait(false);
    }

    /// <summary>Builds the domain record that carries a member's name, kind and implementation.</summary>
    /// <typeparam name="T">The implementation configuration being written.</typeparam>
    /// <param name="implementation">Which implementation this member is.</param>
    /// <param name="name">The member's name.</param>
    /// <param name="implementationConfiguration">The implementation's own configuration.</param>
    /// <returns>The domain record to save.</returns>
    protected abstract TDomainConfiguration Compose<T>(
        string implementation, string name, T implementationConfiguration)
        where T : TImplementationConfiguration;

    /// <inheritdoc />
    Task<IGenericResult> IDomainConfigurationProvider<TImplementationConfiguration>.Delete(
        Guid id, CancellationToken cancellationToken) => Delete(id, cancellationToken);

    /// <inheritdoc />
    Task<IGenericResult> IDomainConfigurationProvider<TImplementationConfiguration>.Delete(
        string name, CancellationToken cancellationToken) => Delete(name, cancellationToken);

}
