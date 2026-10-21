using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// What the retention policies and the legal holds share: the gateway of the tenant tables and the read of an organization's rows (FDW-1575).
/// </summary>
internal sealed class RetentionStore
{
    private readonly IConfigurationGatewayProvider _gateways;
    private readonly ILogger _logger;

    internal RetentionStore(IConfigurationGatewayProvider gateways, RetentionTables tables, ILogger logger)
    {
        _gateways = gateways ?? throw new ArgumentNullException(nameof(gateways));
        Tables = tables ?? throw new ArgumentNullException(nameof(tables));
        _logger = logger;
    }

    internal RetentionTables Tables { get; }

    internal IGenericResult<IConfigurationGateway> Gateway() => _gateways.Get(Tables.DataStoreName);

    // The rows of one organization in a table, or the failure to read them, named by what they are.
    internal async Task<IGenericResult<IReadOnlyList<TRow>>> Rows<TRow>(
        string table, Guid tenantId, Guid organizationId, string what, CancellationToken cancellationToken)
    {
        var gateway = Gateway();
        if (gateway.IsFailure || gateway.Value is not { } resolved)
        {
            return GenericResult<IReadOnlyList<TRow>>.Failure(RetentionLog.ReadFailed(_logger, what, gateway.CurrentMessage));
        }

        var read = await resolved.Execute<IEnumerable<TRow>>(
            Query.From<TRow>(Tables.DataStoreName, Tables.PathName, table).Where("TenantId", tenantId).Where("OrganizationId", organizationId).Build(),
            cancellationToken).ConfigureAwait(false);
        return read.IsSuccess && read.Value is { } rows
            ? GenericResult<IReadOnlyList<TRow>>.Success([.. rows])
            : GenericResult<IReadOnlyList<TRow>>.Failure(RetentionLog.ReadFailed(_logger, what, read.CurrentMessage));
    }
}
