using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
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
    private static string DataStoreName => PlatformConnection.Name;
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

    /// <summary>
    /// Queries every org-tier access grant held by a user, across all tenants and orgs.
    /// </summary>
    /// <param name="userId">The user identifier (matches the <c>sub</c> claim in the JWT).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All grant rows for the user, or a failure result.</returns>
    /// <remarks>
    /// Why this is not RLS-filtered down to the active tenant: no security policy attaches a
    /// predicate to <c>tenant.*</c>. It cannot — <c>security.fn_TenantFilter</c> itself reads
    /// <c>tenant.TenantOrgAccess</c> to decide every other table's visibility, so a policy on it
    /// would be circular under SCHEMABINDING. That is what makes enumerating a user's memberships
    /// answerable at all; the same query against a tenant-scoped table would silently return only
    /// the active tenant and read as "this user belongs to one tenant".
    /// </remarks>
    public virtual async Task<IGenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>> Get(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        TenantOrgAccessConfigurationProviderLog.GetByUserTrace(_logger, userId);

        var command = Query.From<TenantOrgAccessConfiguration>(DataStoreName, PathName, ContainerName)
            .Where("UserId", userId)
            .Build();

        var gateway = Gateway();
        if (gateway.IsFailure || gateway.Value is not { } resolved)
            return GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Failure(
                TenantOrgAccessConfigurationProviderLog.GetByUserFailed(_logger, userId,
                    new InvalidOperationException(gateway.CurrentMessage)));

        var result = await resolved.Execute<IEnumerable<TenantOrgAccessConfiguration>>(command, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Failure(
                TenantOrgAccessConfigurationProviderLog.GetByUserFailed(_logger, userId,
                    new InvalidOperationException(result.CurrentMessage)));
        }

        var grants = result.Value?.ToList() ?? [];
        TenantOrgAccessConfigurationProviderLog.GetByUserLoaded(_logger, userId, grants.Count);
        return GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Success(grants);
    }

    /// <summary>
    /// Lists every access row of an organization (FDW-1593): one per person and permission.
    /// </summary>
    /// <param name="tenantId">The tenant the organization belongs to.</param>
    /// <param name="orgId">The organization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rows, or a failure result.</returns>
    public virtual async Task<IGenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>> Members(
        Guid tenantId,
        Guid orgId,
        CancellationToken cancellationToken = default)
    {
        var gateway = Gateway();
        if (gateway.IsFailure || gateway.Value is not { } resolved)
            return GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Failure(
                OrganizationAccessLog.ReadFailed(_logger, $"The access rows of organization '{orgId}'", gateway.CurrentMessage));

        var read = await resolved.Execute<IEnumerable<TenantOrgAccessConfiguration>>(
            Query.From<TenantOrgAccessConfiguration>(DataStoreName, PathName, ContainerName)
                .Where("TenantId", tenantId)
                .Where("OrgId", orgId)
                .Build(),
            cancellationToken).ConfigureAwait(false);
        return read.IsSuccess && read.Value is { } rows
            ? GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Success([.. rows])
            : GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Failure(
                OrganizationAccessLog.ReadFailed(_logger, $"The access rows of organization '{orgId}'", read.CurrentMessage));
    }

    /// <summary>
    /// Gives a person exactly the permissions of a role in an organization (FDW-1593): their rows there are removed and one row per
    /// permission is written, with the role's name on each.
    /// </summary>
    /// <param name="userId">The person.</param>
    /// <param name="tenantId">The tenant the organization belongs to.</param>
    /// <param name="orgId">The organization.</param>
    /// <param name="roleName">The role, written on every row.</param>
    /// <param name="permissions">The permissions of the role.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or a failure result. A failure after the old rows were removed leaves the person with fewer permissions, never more.</returns>
    /// <remarks>
    /// The table is a materialised access table that the row-security filters and the sign-in read, so its rows are removed rather than
    /// marked: a marked row would still be read. There is no transaction across the writes; the order (remove, then write) is the one
    /// whose failure grants nothing the role does not.
    /// </remarks>
    public virtual async Task<IGenericResult> Replace(
        Guid userId,
        Guid tenantId,
        Guid orgId,
        string roleName,
        IReadOnlyList<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var cleared = await Clear(userId, tenantId, orgId, cancellationToken).ConfigureAwait(false);
        if (cleared.IsFailure)
            return cleared;

        var gateway = Gateway();
        if (gateway.IsFailure || gateway.Value is not { } resolved)
            return GenericResult.Failure(OrganizationAccessLog.WriteFailed(_logger, $"The access rows of user '{userId}'", gateway.CurrentMessage));

        foreach (var permission in permissions)
        {
            var written = await resolved.Execute<int>(
                Insert.Into<TenantOrgAccessConfiguration>(ContainerName)
                    .DataStore(DataStoreName)
                    .Path(PathName)
                    .Value(new TenantOrgAccessConfiguration
                    {
                        UserId = userId, TenantId = tenantId, OrgId = orgId, RoleName = roleName, PermissionName = permission,
                    }),
                cancellationToken).ConfigureAwait(false);
            if (written.IsFailure)
                return GenericResult.Failure(OrganizationAccessLog.WriteFailed(_logger, $"The access row '{permission}' of user '{userId}'", written.CurrentMessage));
        }

        return GenericResult.Success();
    }

    /// <summary>
    /// Takes a person out of an organization (FDW-1593): every access row they hold there is removed.
    /// </summary>
    /// <param name="userId">The person.</param>
    /// <param name="tenantId">The tenant the organization belongs to.</param>
    /// <param name="orgId">The organization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success (also when they held none), or a failure result.</returns>
    public virtual async Task<IGenericResult> Clear(
        Guid userId,
        Guid tenantId,
        Guid orgId,
        CancellationToken cancellationToken = default)
    {
        var gateway = Gateway();
        if (gateway.IsFailure || gateway.Value is not { } resolved)
            return GenericResult.Failure(OrganizationAccessLog.WriteFailed(_logger, $"The access rows of user '{userId}'", gateway.CurrentMessage));

        var removed = await resolved.Execute<int>(
            Delete.From(ContainerName)
                .DataStore(DataStoreName)
                .Path(PathName)
                .Where("UserId", userId)
                .Where("TenantId", tenantId)
                .Where("OrgId", orgId)
                .Build(),
            cancellationToken).ConfigureAwait(false);
        return removed.IsSuccess
            ? GenericResult.Success()
            : GenericResult.Failure(OrganizationAccessLog.WriteFailed(_logger, $"The access rows of user '{userId}'", removed.CurrentMessage));
    }
}
