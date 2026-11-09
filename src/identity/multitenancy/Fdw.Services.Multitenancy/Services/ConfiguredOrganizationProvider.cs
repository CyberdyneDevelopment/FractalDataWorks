using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Configuration;
using Fdw.Services.Multitenancy.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// Reads an organization from <c>tenant.Organization</c> through the configuration gateway (FDW-4573).
/// </summary>
/// <remarks>
/// For a host that runs the single-tenant type: it resolves no tenant per request, but it serves objects that belong to an organization (a pipeline is made in one), and the check
/// that an organization exists, belongs to a tenant and is active needs a real read. The SQL multitenancy type registers its own provider for the hosts that resolve tenants.
/// </remarks>
public sealed class ConfiguredOrganizationProvider : IOrganizationProvider
{
    private readonly IConfigurationGatewayProvider _gateways;
    private readonly string _dataStoreName;
    private readonly string _pathName;
    private readonly string _containerName;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ConfiguredOrganizationProvider"/> class.</summary>
    /// <param name="gateways">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The configuration connection the organization rows live on.</param>
    /// <param name="pathName">The schema the rows live in.</param>
    /// <param name="containerName">The organization table.</param>
    /// <param name="logger">The logger.</param>
    public ConfiguredOrganizationProvider(
        IConfigurationGatewayProvider gateways, string dataStoreName, string pathName, string containerName, ILogger<ConfiguredOrganizationProvider>? logger = null)
    {
        _gateways = gateways ?? throw new ArgumentNullException(nameof(gateways));
        _dataStoreName = dataStoreName ?? throw new ArgumentNullException(nameof(dataStoreName));
        _pathName = pathName ?? throw new ArgumentNullException(nameof(pathName));
        _containerName = containerName ?? throw new ArgumentNullException(nameof(containerName));
        _logger = logger ?? NullLogger<ConfiguredOrganizationProvider>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<OrganizationConfiguration>> Get(Guid orgId, CancellationToken cancellationToken = default)
    {
        var rows = await Read(q => q.Where("Id", orgId), cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure)
        {
            return rows.ToNewResult<OrganizationConfiguration>();
        }

        return FirstOf(rows.Value!) is { } row
            ? GenericResult<OrganizationConfiguration>.Success(ToConfiguration(row))
            : GenericResult<OrganizationConfiguration>.Failure(OrganizationLookupLog.OrganizationNotFound(_logger, orgId));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<OrganizationConfiguration>> GetDefault(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var rows = await Read(q => q.Where("TenantId", tenantId).Where("IsDefault", true), cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure)
        {
            return rows.ToNewResult<OrganizationConfiguration>();
        }

        return FirstOf(rows.Value!) is { } row
            ? GenericResult<OrganizationConfiguration>.Success(ToConfiguration(row))
            : GenericResult<OrganizationConfiguration>.Failure(OrganizationLookupLog.NoDefaultOrganization(_logger, tenantId));
    }

    // The first row, or none: the list is indexable, so it is indexed (CA1826).
    private static OrganizationRow? FirstOf(IReadOnlyList<OrganizationRow> rows) => rows.Count > 0 ? rows[0] : null;

    // The current, non-deleted rows a filter selects.
    private async Task<IGenericResult<IReadOnlyList<OrganizationRow>>> Read(
        Func<QueryCommandBuilder<OrganizationRow>, QueryCommandBuilder<OrganizationRow>> filter, CancellationToken cancellationToken)
    {
        var found = _gateways.Get(_dataStoreName);
        if (found.IsFailure || found.Value is not { } gateway)
        {
            return GenericResult<IReadOnlyList<OrganizationRow>>.Failure(OrganizationLookupLog.StoreUnavailable(_logger, found.CurrentMessage ?? string.Empty));
        }

        var query = filter(Query.From<OrganizationRow>(_dataStoreName, _pathName, _containerName)).Where("IsCurrent", true).Where("IsDeleted", false).Build();
        var read = await gateway.Execute<IEnumerable<OrganizationRow>>(query, cancellationToken).ConfigureAwait(false);
        return read.IsSuccess && read.Value is { } rows
            ? GenericResult<IReadOnlyList<OrganizationRow>>.Success([.. rows])
            : GenericResult<IReadOnlyList<OrganizationRow>>.Failure(OrganizationLookupLog.ReadFailed(_logger, read.CurrentMessage ?? string.Empty));
    }

    private static OrganizationConfiguration ToConfiguration(OrganizationRow row) => new()
    {
        Id = row.Id,
        TenantId = row.TenantId,
        Name = row.Name,
        Slug = row.Slug,
        IsDefault = row.IsDefault,
        IsAdminOrg = row.IsAdminOrg,
        IsActive = row.IsActive,
        DefaultWorkspaceId = row.DefaultWorkspaceId,
        VisibilityGroupId = row.VisibilityGroupId,
        IsCurrent = row.IsCurrent,
        IsDeleted = row.IsDeleted,
    };
}
