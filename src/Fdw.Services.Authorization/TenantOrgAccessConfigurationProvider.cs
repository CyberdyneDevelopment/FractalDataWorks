using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>
/// Owns all ConfigurationDb gateway access for the <c>tenant.TenantOrgAccess</c> table.
/// Services in the Authorization domain inject this provider — never IConfigurationGateway directly.
/// </summary>
public class TenantOrgAccessConfigurationProvider
{
    private const string DataStoreName = "PlatformConfiguration";
    private const string PathName = "tenant";
    private const string ContainerName = "TenantOrgAccess";

    private readonly IConfigurationGatewayProvider _gatewayProvider;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantOrgAccessConfigurationProvider"/> class.
    /// </summary>
    /// <param name="gatewayProvider">Resolves the gateway for this provider's store.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <remarks>
    /// The provider selects its own gateway rather than being handed one. Which connection this
    /// domain reads is its own business and is named once here, in <see cref="DataStoreName"/> —
    /// a caller cannot supply a gateway over some other store.
    /// </remarks>
    public TenantOrgAccessConfigurationProvider(
        IConfigurationGatewayProvider gatewayProvider,
        ILogger<TenantOrgAccessConfigurationProvider>? logger = null)
    {
        _gatewayProvider = gatewayProvider ?? throw new ArgumentNullException(nameof(gatewayProvider));
        _logger = logger ?? NullLogger<TenantOrgAccessConfigurationProvider>.Instance;
    }

    /// <summary>The gateway over this provider's store.</summary>
    private IGenericResult<IConfigurationGateway> Gateway() => _gatewayProvider.Get(DataStoreName);

    /// <summary>
    /// Queries org-tier access grants for a specific user within a tenant and org.
    /// </summary>
    /// <param name="userId">The user identifier (matches the <c>sub</c> claim in the JWT).</param>
    /// <param name="tenantId">The tenant to scope the query to.</param>
    /// <param name="orgId">The org to scope the query to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All matching grant rows, or a failure result.</returns>
    public virtual async Task<IGenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>> Get(
        Guid userId,
        Guid tenantId,
        Guid orgId,
        CancellationToken cancellationToken = default)
    {
        TenantOrgAccessConfigurationProviderLog.GetTrace(_logger, userId, tenantId, orgId);

        var command = Query.From<TenantOrgAccessConfiguration>(DataStoreName, PathName, ContainerName)
            .Where("UserId", userId)
            .Where("TenantId", tenantId)
            .Where("OrgId", orgId)
            .Build();

        var gateway = Gateway();
        if (gateway.IsFailure || gateway.Value is not { } resolved)
            return GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Failure(
                TenantOrgAccessConfigurationProviderLog.GetFailed(_logger, userId, orgId,
                    new InvalidOperationException(gateway.CurrentMessage)));

        var result = await resolved.Execute<IEnumerable<TenantOrgAccessConfiguration>>(command, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Failure(
                TenantOrgAccessConfigurationProviderLog.GetFailed(_logger, userId, orgId,
                    new InvalidOperationException(result.CurrentMessage)));
        }

        var grants = result.Value?.ToList() ?? [];
        TenantOrgAccessConfigurationProviderLog.GetLoaded(_logger, userId, orgId, grants.Count);
        return GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Success(grants);
    }
}
