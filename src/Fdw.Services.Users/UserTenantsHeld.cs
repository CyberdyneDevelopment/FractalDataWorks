using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users.Abstractions;
using Fdw.Services.Users.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Users;

/// <summary>
/// The default <see cref="IUserTenantsHeld"/>: the memberships that name a person are read from the user tenant implementation table by the person's
/// id, and each is kept only if its domain row is still the current one and not deleted (FDW-2676).
/// </summary>
/// <remarks>
/// The person's id is a uniqueidentifier column, so it is asked for as the Guid it is. A person who holds more than <see cref="MaxMemberships"/>
/// memberships fails the read, as the lookup of their domain rows carries a key for each.
/// </remarks>
public sealed class UserTenantsHeld : IUserTenantsHeld
{
    /// <summary>The most memberships one person may hold for a read to answer.</summary>
    public const int MaxMemberships = 1000;

    private static readonly string[] ByRowId = ["RowId"];

    private readonly ConfigurationRowReader _memberships;
    private readonly ConfigurationRowReader _domains;
    private readonly ILogger<UserTenantsHeld> _logger;

    /// <summary>Initializes a new instance of the <see cref="UserTenantsHeld"/> class.</summary>
    /// <param name="gateways">Reaches the configuration store.</param>
    /// <param name="dataStoreName">The data store the tables are in.</param>
    /// <param name="pathName">The path of the user tenant tables.</param>
    /// <param name="logger">The logger.</param>
    public UserTenantsHeld(IConfigurationGatewayProvider gateways, string dataStoreName, string pathName, ILogger<UserTenantsHeld>? logger)
    {
        _logger = logger ?? NullLogger<UserTenantsHeld>.Instance;
        _memberships = new ConfigurationRowReader(gateways, dataStoreName, pathName, "UserTenantsConfiguration", _logger);
        _domains = new ConfigurationRowReader(gateways, dataStoreName, pathName, "UserTenants", _logger);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<HeldTenant>>> Get(Guid userId, CancellationToken cancellationToken = default)
    {
        var asked = await _memberships.Page(Condition("UserId", "Equal", userId), ByRowId, 0, MaxMemberships + 1, true, cancellationToken).ConfigureAwait(false);
        if (asked.IsFailure)
        {
            return asked.ToNewResult<IReadOnlyList<HeldTenant>>();
        }

        if (asked.Value!.Count > MaxMemberships)
        {
            return GenericResult<IReadOnlyList<HeldTenant>>.Failure(UserTenantsHeldLog.TooManyMemberships(_logger, userId.ToString("D"), MaxMemberships));
        }

        if (asked.Value.Count == 0)
        {
            UserTenantsHeldLog.Read(_logger, userId.ToString("D"), 0);
            return GenericResult<IReadOnlyList<HeldTenant>>.Success([]);
        }

        try
        {
            var keys = asked.Value.Select(row => row.Number("UserTenantsRowId") ?? throw new InvalidOperationException("a membership has no domain row")).Distinct().ToList();
            var current = await _domains.Page(Condition("RowId", "In", keys.Cast<object>().ToList()), ByRowId, 0, keys.Count, true, cancellationToken).ConfigureAwait(false);
            if (current.IsFailure)
            {
                return current.ToNewResult<IReadOnlyList<HeldTenant>>();
            }

            var present = current.Value!.Select(row => row.Number("RowId")).ToHashSet();
            var held = asked.Value
                .Where(row => present.Contains(row.Number("UserTenantsRowId")))
                .Select(row => new HeldTenant(
                    row.Identifier("TenantId") ?? throw new InvalidOperationException("a membership has no tenant"),
                    row.Flag("IsDefault") ?? throw new InvalidOperationException("a membership does not say whether it is the default")))
                .ToList();
            UserTenantsHeldLog.Read(_logger, userId.ToString("D"), held.Count);
            return GenericResult<IReadOnlyList<HeldTenant>>.Success(held);
        }
        catch (InvalidOperationException unreadable)
        {
            return GenericResult<IReadOnlyList<HeldTenant>>.Failure(UserTenantsHeldLog.MembershipUnreadable(_logger, userId.ToString("D"), unreadable.Message));
        }
    }

    private static FilterCondition Condition(string column, string operatorName, object value)
        => new() { PropertyName = column, Operator = FilterOperators.ByName(operatorName), Value = value };
}
