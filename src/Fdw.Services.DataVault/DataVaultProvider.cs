using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services;
using Fdw.Services.DataVault.Abstractions;
using Fdw.Services.DataVault.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.DataVault;

/// <summary>
/// Default implementation of <see cref="IDataVaultProvider"/>, adding the typed
/// <see cref="Get(DataVaultRequest, CancellationToken)"/> entry point over
/// <see cref="PlatformServiceProviderBase{TService,TConfiguration,TFactory,TConfigurationProvider}"/>.
/// </summary>
/// <remarks>
/// Holds no implementation dependencies — it loads configuration and dispatches by
/// ServiceOptionType exactly like every other domain provider (see <c>ConnectionProvider</c>).
/// <see cref="Get(DataVaultRequest, CancellationToken)"/> only adds the empty-request validation the
/// base <c>Get(string)</c>/<c>Get(Guid)</c> overloads don't have, then delegates to them; the base's
/// <c>CreateFrom</c> already prefers a factory's <see cref="IAsyncServiceFactory{TService}"/> overload
/// when present, which is how the registered vault factory resolves its own connection and pepper.
/// </remarks>
public sealed class DataVaultProvider
    : PlatformServiceProviderBase<
          IDataVault,
          IDataVaultImplementationConfiguration,
          IDataVaultFactory<IDataVault, IDataVaultImplementationConfiguration>,
          IDataVaultConfigurationProvider>,
      IDataVaultProvider
{
    private readonly ILogger<DataVaultProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataVaultProvider"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="services">The container this provider resolves factories from.</param>
    public DataVaultProvider(IServiceProvider services, ILogger<DataVaultProvider> logger)
        : base(services, logger ?? NullLogger<DataVaultProvider>.Instance)
    {
        _logger = logger ?? NullLogger<DataVaultProvider>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IDataVault>> Get(DataVaultRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || (request.Id is null && string.IsNullOrWhiteSpace(request.Name)))
            return GenericResult<IDataVault>.Failure(DataVaultLog.EmptyVaultRequest(_logger));

        return request.Id.HasValue
            ? await Get(request.Id.Value, cancellationToken).ConfigureAwait(false)
            : await Get(request.Name!, cancellationToken).ConfigureAwait(false);
    }
}
