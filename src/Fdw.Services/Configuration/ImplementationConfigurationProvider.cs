using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Configuration;

/// <summary>
/// An implementation's configuration provider: one implementation's rows, keyed by the domain row
/// that owns them.
/// </summary>
/// <typeparam name="TContract">The domain's implementation configuration contract.</typeparam>
/// <typeparam name="TConfig">The concrete configuration this provider supplies.</typeparam>
/// <typeparam name="TCommand">The configuration command for those rows.</typeparam>
/// <remarks>
/// It is typed to the domain's <i>contract</i> as well as its own concrete configuration, which is what
/// lets one domain hold every implementation provider it has in a single dictionary.
/// </remarks>
public class ImplementationConfigurationProvider<TContract, TConfig, TCommand>
    : ImplementationConfigurationProviderBase<TConfig, TCommand>,
      IImplementationConfigurationProvider<TContract>
    where TContract : IImplementationConfiguration
    where TConfig : class, TContract
    where TCommand : ConfigurationCommandBase<TConfig>
{

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ImplementationConfigurationProvider{TContract, TConfig, TCommand}"/> class.
    /// </summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the named connection.</param>
    /// <param name="dataStoreName">The connection the rows live in.</param>
    /// <param name="pathName">The schema the rows live in.</param>
    public ImplementationConfigurationProvider(
        ILogger<ImplementationConfigurationProviderBase<TConfig, TCommand>>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName)
        : base(logger, gatewayProvider, dataStoreName, pathName)
    {
    }

    /// <inheritdoc />
    async Task<IGenericResult<TContract>> IImplementationConfigurationProvider<TContract>.Get(
        Guid domainId, CancellationToken cancellationToken)
        => Widen(await Get(domainId, cancellationToken).ConfigureAwait(false));

    /// <inheritdoc />
    async Task<IGenericResult<IReadOnlyList<TContract>>> IImplementationConfigurationProvider<TContract>.Get(
        CancellationToken cancellationToken)
    {
        var result = await Get(cancellationToken).ConfigureAwait(false);
        return result.IsSuccess && result.Value is not null
            ? GenericResult<IReadOnlyList<TContract>>.Success([.. result.Value])
            : result.ToNewResult<IReadOnlyList<TContract>>();
    }

    /// <inheritdoc />
    async Task<IGenericResult<TContract>> IImplementationConfigurationProvider<TContract>.Save(
        TContract record, CancellationToken cancellationToken)
        => record is TConfig typed
            ? Widen(await Save(typed, cancellationToken).ConfigureAwait(false))
            : GenericResult<TContract>.Failure(
                ServicesResultCodes.ByName("InvalidConfigurationType"),
                ResultDetails.Create("ExpectedType", typeof(TConfig).Name,
                                     "ActualType", record?.GetType().Name ?? "(null)"));

    /// <inheritdoc />
    Task<IGenericResult> IImplementationConfigurationProvider<TContract>.Delete(
        Guid domainId, CancellationToken cancellationToken) => Delete(domainId, cancellationToken);

    private static IGenericResult<TContract> Widen(IGenericResult<TConfig> result)
        => result.IsSuccess && result.Value is not null
            ? GenericResult<TContract>.Success(result.Value)
            : result.ToNewResult<TContract>();
}
