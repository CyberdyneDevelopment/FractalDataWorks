using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Logging;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Authorization;

/// <summary>
/// Domain configuration provider for user-role assignments.
/// Thin wrapper over <see cref="ImplementationConfigurationProviderBase{TConfig,TCommand}"/> with a by-user convenience method.
/// </summary>
public class UserRoleConfigurationProvider : ImplementationConfigurationProviderBase<UserRoleConfiguration, UserRoleConfigurationCommand>
{
    private readonly ILogger _logger;


    /// <summary>Initializes a new instance of the <see cref="UserRoleConfigurationProvider"/> class.</summary>
    public UserRoleConfigurationProvider(
        ILogger<UserRoleConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "authz")
        : base(logger ?? NullLogger<UserRoleConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
        _logger = logger ?? NullLogger<UserRoleConfigurationProvider>.Instance;
    }

    /// <summary>
    /// Gets all user-role assignments for a specific user.
    /// Returns a failure result if the underlying provider fails — callers must treat failure as
    /// an authorization denial (fail-closed).
    /// </summary>
    public virtual async Task<IGenericResult<IReadOnlyList<UserRoleConfiguration>>> GetByUser(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var allResult = await Get(cancellationToken).ConfigureAwait(false);
        if (!allResult.IsSuccess || allResult.Value is null)
            // Why: FDW-532 — failing to load user role assignments must propagate as a failure
            // result, not silently become an empty list. Callers must fail-closed on this.
            // ToNewResult() preserves the full error chain from the underlying Get() failure.
            return allResult.ToNewResult<IReadOnlyList<UserRoleConfiguration>>();

        // Why: FDW-532 follow-up — userId (token subject) and the stored authz.UserRole.UserId are
        // both GUIDs but differ in case (the DB stores uppercase; Guid.ToString() emits lowercase),
        // so a case-SENSITIVE Ordinal compare matched nobody and zeroed every user's roles (admin
        // included). GUID identity is case-insensitive — compare with OrdinalIgnoreCase.
        var filtered = allResult.Value
            .Where(ur => string.Equals(ur.UserId, userId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        UserRoleConfigurationProviderLog.UserRolesForUserLoaded(_logger, filtered.Count, userId);
        return GenericResult<IReadOnlyList<UserRoleConfiguration>>.Success(filtered);
    }
}
