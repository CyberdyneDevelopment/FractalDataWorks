using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>
/// Reads org-tier access grants from <c>tenant.TenantOrgAccess</c> via
/// <see cref="TenantOrgAccessConfigurationProvider"/>.
/// </summary>
public sealed class DefaultOrgAccessProvider : IOrgAccessProvider
{
    private readonly TenantOrgAccessConfigurationProvider _provider;
    private readonly ILogger<DefaultOrgAccessProvider> _logger;

    /// <summary>Initializes a new instance of <see cref="DefaultOrgAccessProvider"/>.</summary>
    public DefaultOrgAccessProvider(
        TenantOrgAccessConfigurationProvider provider,
        ILogger<DefaultOrgAccessProvider>? logger = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _logger = logger ?? NullLogger<DefaultOrgAccessProvider>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>> Get(
        Guid userId,
        Guid tenantId,
        Guid orgId,
        CancellationToken cancellationToken = default)
    {
        AuthorizationLog.OrgAccessQueryStarted(_logger, userId, orgId);

        var result = await _provider.Get(userId, tenantId, orgId, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Failure(
                AuthorizationLog.OrgAccessQueryFailed(_logger, userId, orgId));
        }

        AuthorizationLog.OrgAccessGrantsLoaded(_logger, result.Value!.Count, userId, orgId);
        return result;
    }
}
