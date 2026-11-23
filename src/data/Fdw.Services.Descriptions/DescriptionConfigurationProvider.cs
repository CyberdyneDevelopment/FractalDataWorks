using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Descriptions;

/// <summary>Supplies the configured descriptions, read from <c>catalog.Description</c> through the configuration gateway.</summary>
public sealed class DescriptionConfigurationProvider
    : DomainConfigurationProviderBase<IDescriptionImplementationConfiguration, DescriptionDomainConfiguration>,
      IDescriptionConfigurationProvider
{
    /// <summary>Initializes the provider.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto <paramref name="dataStoreName"/>.</param>
    /// <param name="dataStoreName">The configuration connection the rows live on.</param>
    /// <param name="pathName">The schema the rows live in.</param>
    /// <param name="containerName">The domain table.</param>
    public DescriptionConfigurationProvider(
        ILogger<DescriptionConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
        _containerName = containerName;
    }

    private readonly string _containerName;

    /// <inheritdoc />
    public async Task<IGenericResult> InUnit(Func<ConfigurationWriteScope, Task<IGenericResult>> work, CancellationToken ct = default)
    {
        var unit = await BeginWriteScope(ct).ConfigureAwait(false);
        if (unit.IsFailure) return unit;

        await using var scope = unit.Value!;
        var done = await work(scope).ConfigureAwait(false);
        if (done.IsFailure)
        {
            await scope.Rollback(ct).ConfigureAwait(false);
            return done;
        }

        return await scope.Commit(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<DescriptionDomainConfiguration>>> ReadCurrent(FilterExpression filter, ConfigurationWriteScope unit, CancellationToken ct = default)
    {
        var read = await unit.Read<IEnumerable<DescriptionDomainConfiguration>>(
            new QueryCommand<DescriptionDomainConfiguration> { Filter = filter }, new DataStoreTarget(DataStoreName, PathName, _containerName), ct).ConfigureAwait(false);
        if (read.IsFailure) return read.ToNewResult<IReadOnlyList<DescriptionDomainConfiguration>>();

        return GenericResult<IReadOnlyList<DescriptionDomainConfiguration>>.Success([.. read.Value ?? []]);
    }
}
