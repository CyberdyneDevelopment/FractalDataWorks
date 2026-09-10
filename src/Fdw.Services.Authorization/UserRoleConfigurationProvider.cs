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
/// Thin wrapper over <see cref="ImplementationConfigurationProviderBase{TDomainConfiguration,TImplementationConfiguration,TCommand}"/> with a by-user convenience method.
/// </summary>
public class UserRoleConfigurationProvider : ImplementationConfigurationProviderBase<UserRoleConfiguration, IUserRoleImplementationConfiguration, UserRoleConfigurationCommand>
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
            return allResult.ToNewResult<IReadOnlyList<UserRoleConfiguration>>();

        // Why IsCurrent and IsDeleted are tested here and not only by the caller: three callers
        // share this method -- EffectivePermissionResolver, DefaultPrincipalResolver's role-claim
        // bake at login, and the user-roles endpoint -- and none of them filtered, so a revoked
        // assignment kept its permissions, kept appearing in the API, and kept being written into
        // the token. Fail-closed belongs at the read that every one of them goes through.
        var filtered = allResult.Value
            .Where(ur => string.Equals(ur.UserId, userId, StringComparison.OrdinalIgnoreCase)
                         && ur.IsCurrent && !ur.IsDeleted)
            .ToList();

        UserRoleConfigurationProviderLog.UserRolesForUserLoaded(_logger, filtered.Count, userId);
        return GenericResult<IReadOnlyList<UserRoleConfiguration>>.Success(filtered);
    }
}
