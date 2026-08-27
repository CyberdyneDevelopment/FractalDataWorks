using System;
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

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ServiceConfigurationProviderBase{TDomainConfiguration, TImplementationConfiguration, TCommand}"/> class.
    /// </summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gateway">The configuration gateway this domain reads from.</param>
    /// <param name="dataStoreName">The store the domain's rows live in.</param>
    /// <param name="pathName">The schema the domain's rows live in.</param>
    protected ServiceConfigurationProviderBase(
        ILogger<ImplementationConfigurationProviderBase<TDomainConfiguration, TCommand>>? logger,
        Lazy<IConfigurationGateway> gateway,
        string dataStoreName,
        string pathName)
        : base(logger, gateway, dataStoreName, pathName)
        => _log = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

    /// <inheritdoc />
    public IGenericResult Register<T>(string name, T implementationConfigurationProvider)
        where T : IImplementationConfigurationProvider<TImplementationConfiguration>
    {
        if (implementationConfigurationProvider is not IServiceConfigurationProvider erased)
        {
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.NoImplementationProvider(_log, name, name));
        }

        base.Register(name, erased);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    async Task<IGenericResult<TImplementationConfiguration>> IDomainConfigurationProvider<TImplementationConfiguration>.Get(
        string name, CancellationToken cancellationToken)
        => Unwrap(await Get(name, cancellationToken).ConfigureAwait(false), name);

    /// <inheritdoc />
    async Task<IGenericResult<TImplementationConfiguration>> IDomainConfigurationProvider<TImplementationConfiguration>.Get(
        Guid id, CancellationToken cancellationToken)
        => Unwrap(await Get(id, cancellationToken).ConfigureAwait(false), id.ToString());

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

    // Why the composed record is unwrapped rather than returned: the caller asked the domain which
    // implementation this member is, and the answer is that implementation's configuration. The domain
    // record is how it was found, not what was wanted.
    private IGenericResult<TImplementationConfiguration> Unwrap(
        IGenericResult<TDomainConfiguration> composed, string identifier)
    {
        if (!composed.IsSuccess || composed.Value is null)
            return composed.ToNewResult<TImplementationConfiguration>();

        return composed.Value is IServiceDispatchHost host
            && host.ServiceDispatchBody is TImplementationConfiguration implementation
            ? GenericResult<TImplementationConfiguration>.Success(implementation)
            : GenericResult<TImplementationConfiguration>.Failure(
                DefaultConfigurationProviderLog.NoImplementationProvider(
                    _log, identifier, composed.Value.ServiceOptionType ?? "(none)"));
    }
}
