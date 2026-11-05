using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections;

/// <summary>Supplies the ConnectionAssignment configuration (FDW-1581).</summary>
public sealed class ConnectionAssignmentConfigurationProvider
    : DomainConfigurationProviderBase<IConnectionAssignmentImplementationConfiguration>,
      IConnectionAssignmentConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionAssignmentConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public ConnectionAssignmentConfigurationProvider(
        ILogger<ConnectionAssignmentConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Supersede(
        ConnectionAssignmentImplementationConfiguration superseded,
        ConnectionAssignmentImplementationConfiguration replacement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(superseded);
        ArgumentNullException.ThrowIfNull(replacement);

        var scope = await BeginWriteScope(cancellationToken).ConfigureAwait(false);
        if (scope.IsFailure) return scope;

        await using var unit = scope.Value!;

        var retired = await Save(superseded, DomainName, DomainName, superseded.Name, unit, cancellationToken).ConfigureAwait(false);
        if (retired.IsFailure)
        {
            await unit.Rollback(cancellationToken).ConfigureAwait(false);
            return retired;
        }

        var saved = await Save(replacement, DomainName, DomainName, replacement.Name, unit, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            await unit.Rollback(cancellationToken).ConfigureAwait(false);
            return saved;
        }

        return await unit.Commit(cancellationToken).ConfigureAwait(false);
    }

    private const string DomainName = "ConnectionAssignment";
}
