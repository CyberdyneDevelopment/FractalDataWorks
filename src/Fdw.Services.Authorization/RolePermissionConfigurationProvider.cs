using Fdw.Results;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System;
using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the RolePermission configuration.</summary>
public sealed class RolePermissionConfigurationProvider
    : DomainConfigurationProviderBase<IRolePermissionImplementationConfiguration>,
      IRolePermissionConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="RolePermissionConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public RolePermissionConfigurationProvider(
        ILogger<RolePermissionConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> ReplacePermissions(
        IReadOnlyList<Guid> retiring,
        IReadOnlyList<IRolePermissionImplementationConfiguration> mappings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(retiring);
        ArgumentNullException.ThrowIfNull(mappings);

        var scope = await BeginWriteScope(cancellationToken).ConfigureAwait(false);
        if (scope.IsFailure) return scope;

        await using var unit = scope.Value!;

        for (var i = 0; i < retiring.Count; i++)
        {
            var removed = await Delete(retiring[i], unit, cancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                await unit.Rollback(cancellationToken).ConfigureAwait(false);
                return removed;
            }
        }

        for (var i = 0; i < mappings.Count; i++)
        {
            var saved = await Save(mappings[i], DomainName, DomainName, mappings[i].Name, unit, cancellationToken)
                .ConfigureAwait(false);
            if (saved.IsFailure)
            {
                await unit.Rollback(cancellationToken).ConfigureAwait(false);
                return saved;
            }
        }

        return await unit.Commit(cancellationToken).ConfigureAwait(false);
    }

    // The domain and the implementation are both named for the record itself; the endpoint used to
    // carry them as its own constants.
    private const string DomainName = "RolePermission";
}
