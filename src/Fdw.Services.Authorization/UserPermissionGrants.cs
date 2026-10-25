using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Logging;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>
/// The default <see cref="IUserPermissionGrants"/>: the rows of the <c>UserPermissionGrant</c> view that name a person (FDW-2688).
/// </summary>
/// <remarks>
/// The view applies the version predicates of every table it joins, so the rows are read as they are and no current-row filter is added.
/// The person's id is asked for in both spellings of a Guid, since the text may have been written in either, and each row is then
/// compared as the Guid it is (FDW-532). A person whose roles grant more than <see cref="MaxRows"/> rows fails the read.
/// </remarks>
public sealed class UserPermissionGrants : IUserPermissionGrants
{
    /// <summary>The most rows one person's grants may hold for a read to answer.</summary>
    public const int MaxRows = 20000;

    private static readonly string[] ByRoleThenPermission = ["RoleName", "PermissionName"];

    // The tables the view authz.UserPermissionGrant is made of. A role given or taken away, or a role's permissions changed, is a write to one
    // of them; the cached page of a person's grants is dropped by it, so the next sign-in reads what the roles are now.
    private static readonly string[] ViewTables =
    [
        "UserRole", "UserRoleConfiguration", "Role", "RoleConfiguration",
        "RolePermission", "RolePermissionConfiguration", "Permission", "PermissionConfiguration",
    ];

    private readonly ConfigurationRowReader _rows;
    private readonly ILogger<UserPermissionGrants> _logger;

    /// <summary>Initializes a new instance of the <see cref="UserPermissionGrants"/> class.</summary>
    /// <param name="gateways">Reaches the configuration store.</param>
    /// <param name="dataStoreName">The data store the view is in.</param>
    /// <param name="pathName">The path of the authorization tables and views.</param>
    /// <param name="logger">The logger.</param>
    public UserPermissionGrants(IConfigurationGatewayProvider gateways, string dataStoreName, string pathName, ILogger<UserPermissionGrants>? logger)
    {
        _logger = logger ?? NullLogger<UserPermissionGrants>.Instance;
        _rows = new ConfigurationRowReader(gateways, dataStoreName, pathName, "UserPermissionGrant", _logger, ViewTables);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<UserPermissionGrant>>> Get(Guid userId, CancellationToken cancellationToken = default)
    {
        var asked = await _rows.Page(
            new FilterCondition
            {
                PropertyName = "UserId",
                Operator = FilterOperators.ByName("In"),
                Value = new List<object> { userId.ToString("D").ToLowerInvariant(), userId.ToString("D").ToUpperInvariant() },
            },
            ByRoleThenPermission, 0, MaxRows + 1, false, cancellationToken).ConfigureAwait(false);
        if (asked.IsFailure)
        {
            return asked.ToNewResult<IReadOnlyList<UserPermissionGrant>>();
        }

        if (asked.Value!.Count > MaxRows)
        {
            return GenericResult<IReadOnlyList<UserPermissionGrant>>.Failure(UserPermissionGrantsLog.TooManyRows(_logger, userId.ToString("D"), MaxRows));
        }

        try
        {
            var grants = asked.Value
                .Where(row => Guid.TryParse(row.Text("UserId"), out var holder) && holder == userId)
                .Select(row => new UserPermissionGrant(
                    row.Identifier("AssignmentTenantId") ?? throw new InvalidOperationException("a grant has no assignment tenant"),
                    row.Moment("ExpiresAt"),
                    row.Identifier("RoleId") ?? throw new InvalidOperationException("a grant has no role"),
                    row.Text("RoleName") ?? throw new InvalidOperationException("a grant has no role name"),
                    row.Flag("IsTenantScoped") ?? throw new InvalidOperationException("a grant does not say whether its role is tenant scoped"),
                    row.Identifier("RoleTenantId") ?? throw new InvalidOperationException("a grant has no role tenant"),
                    row.Text("PermissionName")))
                .ToList();
            UserPermissionGrantsLog.Read(_logger, userId.ToString("D"), grants.Count);
            return GenericResult<IReadOnlyList<UserPermissionGrant>>.Success(grants);
        }
        catch (InvalidOperationException unreadable)
        {
            return GenericResult<IReadOnlyList<UserPermissionGrant>>.Failure(UserPermissionGrantsLog.RowUnreadable(_logger, userId.ToString("D"), unreadable.Message));
        }
    }
}
