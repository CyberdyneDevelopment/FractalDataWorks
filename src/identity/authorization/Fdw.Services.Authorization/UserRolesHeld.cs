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
/// The default <see cref="IUserRolesHeld"/>: the assignments that name a person are read from the user role implementation table by the person's id,
/// and each is kept only if its domain row is still the current one and not deleted (FDW-2668).
/// </summary>
/// <remarks>
/// The id is asked for in both spellings of a Guid, since the text may have been written in either, and each row is then compared as the Guid it is
/// (FDW-532). A person who holds more than <see cref="MaxAssignments"/> assignments fails the read, as the lookup of their domain rows carries a key
/// for each.
/// </remarks>
public sealed class UserRolesHeld : IUserRolesHeld
{
    /// <summary>The most assignments one person may hold for a read to answer.</summary>
    public const int MaxAssignments = 1000;

    private static readonly string[] ByRowId = ["RowId"];

    private readonly ConfigurationRowReader _assignments;
    private readonly ConfigurationRowReader _domains;
    private readonly ConfigurationRowReader _roles;
    private readonly ILogger<UserRolesHeld> _logger;

    /// <summary>Initializes a new instance of the <see cref="UserRolesHeld"/> class.</summary>
    /// <param name="gateways">Reaches the configuration store.</param>
    /// <param name="dataStoreName">The data store the tables are in.</param>
    /// <param name="pathName">The path of the authorization tables.</param>
    /// <param name="logger">The logger.</param>
    public UserRolesHeld(IConfigurationGatewayProvider gateways, string dataStoreName, string pathName, ILogger<UserRolesHeld>? logger)
    {
        _logger = logger ?? NullLogger<UserRolesHeld>.Instance;
        _assignments = new ConfigurationRowReader(gateways, dataStoreName, pathName, "UserRoleConfiguration", _logger);
        _domains = new ConfigurationRowReader(gateways, dataStoreName, pathName, "UserRole", _logger);
        _roles = new ConfigurationRowReader(gateways, dataStoreName, pathName, "Role", _logger);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<HeldRole>>> Get(Guid userId, CancellationToken cancellationToken = default)
    {
        var asked = await _assignments.Page(
            In("UserId", [userId.ToString("D").ToLowerInvariant(), userId.ToString("D").ToUpperInvariant()]), ByRowId, 0, MaxAssignments + 1, true, cancellationToken)
            .ConfigureAwait(false);
        if (asked.IsFailure)
        {
            return asked.ToNewResult<IReadOnlyList<HeldRole>>();
        }

        if (asked.Value!.Count > MaxAssignments)
        {
            return GenericResult<IReadOnlyList<HeldRole>>.Failure(UserRolesHeldLog.TooManyAssignments(_logger, userId.ToString("D"), MaxAssignments));
        }

        try
        {
            var named = asked.Value.Where(row => Guid.TryParse(row.Text("UserId"), out var holder) && holder == userId).ToList();
            if (named.Count == 0)
            {
                UserRolesHeldLog.Read(_logger, userId.ToString("D"), 0);
                return GenericResult<IReadOnlyList<HeldRole>>.Success([]);
            }

            // An assignment names its domain row by the row's durable Id, which every version of it keeps (FDW-4240): it stays attached through a re-save of the row.
            var keys = named.Select(row => row.Identifier("UserRoleId") ?? throw new InvalidOperationException("an assignment has no domain row")).Distinct().ToList();
            var current = await _domains.Page(In("Id", keys.Cast<object>().ToList()), ByRowId, 0, keys.Count, true, cancellationToken).ConfigureAwait(false);
            if (current.IsFailure)
            {
                return current.ToNewResult<IReadOnlyList<HeldRole>>();
            }

            var present = current.Value!.Select(row => row.Identifier("Id")).ToHashSet();
            var held = named
                .Where(row => present.Contains(row.Identifier("UserRoleId")))
                .Select(row => new HeldRole(
                    row.Identifier("RoleId") ?? throw new InvalidOperationException("an assignment has no role"),
                    row.Identifier("TenantId") ?? throw new InvalidOperationException("an assignment has no tenant"),
                    row.Moment("ExpiresAt")))
                .ToList();

            // Every role an assignment names must still exist (FDW-5222). One that does not is refused by name rather than handed on:
            // a caller that looked it up would find nothing, and the person's profile would fail to read with nothing saying why.
            var missing = await MissingRole(held, cancellationToken).ConfigureAwait(false);
            if (missing.IsFailure)
            {
                return missing.ToNewResult<IReadOnlyList<HeldRole>>();
            }

            if (missing.Value is { } roleId)
            {
                return GenericResult<IReadOnlyList<HeldRole>>.Failure(UserRolesHeldLog.AssignedRoleMissing(_logger, userId.ToString("D"), roleId.ToString("D")));
            }

            UserRolesHeldLog.Read(_logger, userId.ToString("D"), held.Count);
            return GenericResult<IReadOnlyList<HeldRole>>.Success(held);
        }
        catch (InvalidOperationException unreadable)
        {
            return GenericResult<IReadOnlyList<HeldRole>>.Failure(UserRolesHeldLog.AssignmentUnreadable(_logger, userId.ToString("D"), unreadable.Message));
        }
    }

    // The first role an assignment names that has no current, undeleted row, or none when every one is there.
    private async Task<IGenericResult<Guid?>> MissingRole(IReadOnlyList<HeldRole> held, CancellationToken cancellationToken)
    {
        var named = held.Select(role => role.RoleId).Distinct().ToList();
        if (named.Count == 0)
        {
            return GenericResult<Guid?>.Success(null);
        }

        var roles = await _roles.Page(In("Id", named.Cast<object>().ToList()), ByRowId, 0, named.Count, true, cancellationToken).ConfigureAwait(false);
        if (roles.IsFailure)
        {
            return roles.ToNewResult<Guid?>();
        }

        var present = roles.Value!.Select(row => row.Identifier("Id")).ToHashSet();
        foreach (var roleId in named)
        {
            if (!present.Contains(roleId))
            {
                return GenericResult<Guid?>.Success(roleId);
            }
        }

        return GenericResult<Guid?>.Success(null);
    }

    private static FilterCondition In(string column, IReadOnlyList<object> values)
        => new() { PropertyName = column, Operator = FilterOperators.ByName("In"), Value = values.ToList() };
}
