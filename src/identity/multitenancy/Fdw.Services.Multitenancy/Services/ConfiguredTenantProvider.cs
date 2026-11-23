using System;
using System.Collections.Generic;
using System.Linq;
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
/// Answers the tenant facts a host needs from the rows of <c>tenant.Tenants</c> and <c>tenant.UserTenantsConfiguration</c>, through the configuration gateway (FDW-6528).
/// </summary>
/// <remarks>
/// For a host that runs the single-tenant option: it resolves no tenant per request, but it executes runs and fires schedules for the tenant a request or a schedule
/// names, and a run started for a person is judged by whether that person may act in that tenant and whether the tenant is the global one. The null provider failed every
/// lookup, so no run for a person could be judged. This provider reads the tenant and the person's current membership when it is asked, never from the request:
/// <see cref="TenantAccessRule"/> is the answer, the one the SQL provider gives over the same rows. It tells a tenant only by its identity, whether it is active and
/// whether it is global; the questions a host that resolves tenants per request asks (an organization access, a resolution from a request) are not answered here and say so.
/// </remarks>
public sealed class ConfiguredTenantProvider : ITenantProvider
{
    private readonly IConfigurationGatewayProvider _gateways;
    private readonly string _dataStoreName;
    private readonly string _pathName;
    private readonly string _tenantsContainerName;
    private readonly string _userTenantsContainerName;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ConfiguredTenantProvider"/> class.</summary>
    /// <param name="gateways">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The configuration connection the tenant rows live on.</param>
    /// <param name="pathName">The schema the rows live in.</param>
    /// <param name="tenantsContainerName">The tenant table.</param>
    /// <param name="userTenantsContainerName">The membership table (the implementation table of the user-tenant domain).</param>
    /// <param name="logger">The logger.</param>
    public ConfiguredTenantProvider(
        IConfigurationGatewayProvider gateways, string dataStoreName, string pathName, string tenantsContainerName, string userTenantsContainerName,
        ILogger<ConfiguredTenantProvider>? logger = null)
    {
        _gateways = gateways;
        _dataStoreName = dataStoreName;
        _pathName = pathName;
        _tenantsContainerName = tenantsContainerName;
        _userTenantsContainerName = userTenantsContainerName;
        _logger = logger ?? NullLogger<ConfiguredTenantProvider>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<ITenant>> GetTenant(Guid tenantId, CancellationToken ct = default)
    {
        var rows = await ReadTenants(query => query.Where("Id", tenantId), ct).ConfigureAwait(false);
        if (rows.IsFailure)
            return rows.ToNewResult<ITenant>();

        return rows.Value!.Count > 0
            ? GenericResult<ITenant>.Success(ToTenant(rows.Value[0]))
            : GenericResult<ITenant>.Failure(TenantLookupLog.TenantNotFound(_logger, tenantId));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<ITenant>> GetTenantBySlug(string slug, CancellationToken ct = default)
    {
        var rows = await ReadTenants(query => query.Where("Slug", slug), ct).ConfigureAwait(false);
        if (rows.IsFailure)
            return rows.ToNewResult<ITenant>();

        return rows.Value!.Count > 0
            ? GenericResult<ITenant>.Success(ToTenant(rows.Value[0]))
            : GenericResult<ITenant>.Failure(TenantLookupLog.TenantSlugNotFound(_logger, slug));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IEnumerable<ITenant>>> GetActiveTenants(CancellationToken ct = default)
        => await Tenants(query => query.Where("IsActive", true), ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IGenericResult<IEnumerable<ITenant>>> GetAllTenants(CancellationToken ct = default)
        => await Tenants(query => query, ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IGenericResult<IEnumerable<ITenant>>> GetDeletedTenants(CancellationToken ct = default)
        => await Tenants(query => query.Where("IsCurrent", true).Where("IsDeleted", true), ct, live: false).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IGenericResult<bool>> ValidateTenantAccess(Guid tenantId, string userId, CancellationToken ct = default)
    {
        if (!Guid.TryParse(userId, out var person))
            return GenericResult<bool>.Failure(TenantLookupLog.UserIdNotAnId(_logger, userId));

        // The tenants first, then the person's memberships, both read now: a membership removed after a run was queued is a revoked one, and the run is judged on what
        // the store says when it is judged.
        var tenants = await ReadTenants(query => query, ct).ConfigureAwait(false);
        if (tenants.IsFailure)
            return tenants.ToNewResult<bool>();

        var memberships = await Read<UserTenantRow>(
            _userTenantsContainerName,
            query => query.Where("UserId", person).Where("IsCurrent", true).Where("IsDeleted", false),
            reason => TenantLookupLog.MembershipReadFailed(_logger, userId, reason),
            ct).ConfigureAwait(false);
        if (memberships.IsFailure)
            return memberships.ToNewResult<bool>();

        IReadOnlyCollection<TenantFact> facts = [.. tenants.Value!.Select(row => new TenantFact(row.Id, row.IsActive, row.IsGlobal))];
        HashSet<Guid> memberOf = [.. memberships.Value!.Select(row => row.TenantId)];
        return GenericResult<bool>.Success(TenantAccessRule.Allows(tenantId, facts, memberOf));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<bool>> ValidateOrganizationAccess(Guid tenantId, Guid organizationId, string userId, CancellationToken ct = default)
        => Task.FromResult<IGenericResult<bool>>(GenericResult<bool>.Failure(TenantLookupLog.OperationNotAnswered(_logger, nameof(ValidateOrganizationAccess))));

    /// <inheritdoc/>
    public Task<IGenericResult<ITenant>> ResolveTenant(ITenantResolutionContext context, CancellationToken ct = default)
        => Task.FromResult<IGenericResult<ITenant>>(GenericResult<ITenant>.Failure(TenantLookupLog.OperationNotAnswered(_logger, nameof(ResolveTenant))));

    private async Task<IGenericResult<IEnumerable<ITenant>>> Tenants(
        Func<QueryCommandBuilder<TenantRow>, QueryCommandBuilder<TenantRow>> filter, CancellationToken ct, bool live = true)
    {
        var rows = await ReadTenants(filter, ct, live).ConfigureAwait(false);
        return rows.IsSuccess && rows.Value is { } read
            ? GenericResult<IEnumerable<ITenant>>.Success([.. read.Select(ToTenant)])
            : rows.ToNewResult<IEnumerable<ITenant>>();
    }

    // The current, non-deleted tenants a filter selects: a tenant that was deleted or replaced by a newer version is not one a person may act in. The list of deleted tenants
    // is the one read that states its own conditions (live false).
    private Task<IGenericResult<IReadOnlyList<TenantRow>>> ReadTenants(
        Func<QueryCommandBuilder<TenantRow>, QueryCommandBuilder<TenantRow>> filter, CancellationToken ct, bool live = true)
        => Read(
            _tenantsContainerName,
            live ? query => filter(query).Where("IsCurrent", true).Where("IsDeleted", false) : filter,
            reason => TenantLookupLog.ReadFailed(_logger, reason),
            ct);

    // The rows a filter selects, or the one message that says why they could not be read.
    private async Task<IGenericResult<IReadOnlyList<TRow>>> Read<TRow>(
        string containerName, Func<QueryCommandBuilder<TRow>, QueryCommandBuilder<TRow>> filter, Func<string, Fdw.Messages.IGenericMessage> readFailed, CancellationToken ct)
        where TRow : class
    {
        var found = _gateways.Get(_dataStoreName);
        if (found.IsFailure || found.Value is not { } gateway)
            return GenericResult<IReadOnlyList<TRow>>.Failure(TenantLookupLog.StoreUnavailable(_logger, found.CurrentMessage ?? string.Empty));

        var query = filter(Query.From<TRow>(_dataStoreName, _pathName, containerName)).Build();
        var read = await gateway.Execute<IEnumerable<TRow>>(query, ct).ConfigureAwait(false);
        return read.IsSuccess && read.Value is { } rows
            ? GenericResult<IReadOnlyList<TRow>>.Success([.. rows])
            : GenericResult<IReadOnlyList<TRow>>.Failure(readFailed(read.CurrentMessage ?? string.Empty));
    }

    private static ConfiguredTenant ToTenant(TenantRow row) => new(new TenantConfiguration
    {
        Id = row.Id,
        Name = row.DisplayName,
        Slug = row.Slug,
        OrgPrefix = row.OrgPrefix,
        IsActive = row.IsActive,
        IsGlobal = row.IsGlobal,
        IsSelectable = row.IsSelectable,
        ConnectionName = row.ConnectionName,
    });
}
