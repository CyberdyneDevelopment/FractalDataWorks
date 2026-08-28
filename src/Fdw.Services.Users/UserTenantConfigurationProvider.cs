using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users.Commands;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using CmdBuilders = Fdw.Commands.Data.Extensions;

namespace Fdw.Services.Users;

/// <summary>
/// Domain configuration provider for user-tenant memberships. Sole owner of <c>tenant.UserTenants</c> gatewayProvider access.
/// Thin wrapper over <see cref="ImplementationConfigurationProviderBase{TConfig,TCommand}"/> with
/// tenant-membership query and mutation methods.
/// </summary>
/// <remarks>
/// All reads and writes go through <see cref="IConfigurationGateway"/>. No <see cref="Fdw.Services.Data.Abstractions.IDataGateway"/>
/// usage — tenant.UserTenants is ConfigurationDb data accessed through the config gatewayProvider, same as usr.Users.
/// </remarks>
public class UserTenantConfigurationProvider : ImplementationConfigurationProviderBase<UserTenantConfiguration, UserTenantConfigurationCommand>
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="UserTenantConfigurationProvider"/> class.</summary>
    public UserTenantConfigurationProvider(
        ILogger<UserTenantConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "tenant")
        : base(logger ?? NullLogger<UserTenantConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
        _logger = logger ?? NullLogger<UserTenantConfigurationProvider>.Instance;
    }

    /// <summary>
    /// Gets all tenant identifiers for the specified user.
    /// </summary>
    public virtual async Task<IGenericResult<IReadOnlyList<Guid>>> GetUserTenants(
        Guid userId, CancellationToken cancellationToken = default)
    {
        UserConfigurationProviderLog.LoadTenantsTrace(_logger, userId);

        var command = new QueryCommandBuilder<UserTenantConfiguration>(
                DataStoreName, PathName, "UserTenants")
            .Where(nameof(UserTenantConfiguration.UserId), userId)
            .Where(nameof(UserTenantConfiguration.IsCurrent), true)
            .Where(nameof(UserTenantConfiguration.IsDeleted), false)
            .Build();

        var result = await Execute<IEnumerable<UserTenantConfiguration>>(command, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            UserConfigurationProviderLog.LoadTenantsFailed(_logger, userId);
            return result.Messages.Any()
                ? result.ToNewResult<IReadOnlyList<Guid>>()
                : GenericResult<IReadOnlyList<Guid>>.Failure(UserConfigurationProviderLog.LoadTenantsFailed(_logger, userId));
        }

        var tenantIds = result.Value.Select(ut => ut.TenantId).ToList();
        UserConfigurationProviderLog.LoadTenantsLoaded(_logger, tenantIds.Count, userId);
        return GenericResult<IReadOnlyList<Guid>>.Success(tenantIds);
    }

    /// <summary>
    /// Gets the user's default tenant (the row where <c>IsDefault=1</c>).
    /// Returns a null Value when the user has no active default tenant row.
    /// </summary>
    public virtual async Task<IGenericResult<Guid?>> GetDefaultTenant(
        Guid userId, CancellationToken cancellationToken = default)
    {
        UserConfigurationProviderLog.LoadDefaultTenantTrace(_logger, userId);

        var command = new QueryCommandBuilder<UserTenantConfiguration>(
                DataStoreName, PathName, "UserTenants")
            .Where(nameof(UserTenantConfiguration.UserId), userId)
            .Where(nameof(UserTenantConfiguration.IsDefault), true)
            .Where(nameof(UserTenantConfiguration.IsCurrent), true)
            .Where(nameof(UserTenantConfiguration.IsDeleted), false)
            .Build();

        var result = await Execute<IEnumerable<UserTenantConfiguration>>(command, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            UserConfigurationProviderLog.LoadDefaultTenantFailed(_logger, userId);
            return result.Messages.Any()
                ? result.ToNewResult<Guid?>()
                : GenericResult<Guid?>.Failure(UserConfigurationProviderLog.LoadDefaultTenantFailed(_logger, userId));
        }

        var defaultRow = result.Value?.FirstOrDefault();
        return GenericResult<Guid?>.Success(defaultRow?.TenantId);
    }

    /// <summary>
    /// Grants a user access to a tenant by inserting a new <c>tenant.UserTenants</c> row.
    /// </summary>
    public virtual async Task<IGenericResult> GrantTenantAccess(
        Guid userId, Guid tenantId, bool isDefault = false, CancellationToken cancellationToken = default)
    {
        UserConfigurationProviderLog.GrantTenantTrace(_logger, tenantId, userId);

        var record = new UserTenantConfiguration
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            TenantId = tenantId,
            IsDefault = isDefault,
            IsCurrent = true,
            IsDeleted = false,
        };

        var command = CmdBuilders.Insert.Into<UserTenantConfiguration>("UserTenants")
            .DataStore(DataStoreName).Path(PathName)
            .Value(record);

        var result = await Execute<int>(command, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            return result.Messages.Any()
                ? (IGenericResult)result
                : GenericResult.Failure(UserConfigurationProviderLog.GrantTenantFailed(_logger, tenantId, userId));

        return GenericResult.Success();
    }

    /// <summary>
    /// Revokes a user's access to a tenant by deleting all matching rows.
    /// </summary>
    public virtual async Task<IGenericResult> RevokeTenantAccess(
        Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        UserConfigurationProviderLog.RevokeTenantTrace(_logger, tenantId, userId);

        var command = CmdBuilders.Delete.From("UserTenants")
            .DataStore(DataStoreName).Path(PathName)
            .Where(nameof(UserTenantConfiguration.UserId), userId)
            .Where(nameof(UserTenantConfiguration.TenantId), tenantId)
            .Build();

        var result = await Execute<int>(command, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            return result.Messages.Any()
                ? (IGenericResult)result
                : GenericResult.Failure(UserConfigurationProviderLog.RevokeTenantFailed(_logger, tenantId, userId));

        return GenericResult.Success();
    }

    /// <summary>
    /// Sets the specified tenant as the user's default, clearing any prior default flag.
    /// </summary>
#pragma warning disable MA0051 // Why: sequential fail-loud steps read top-to-bottom; splitting hurts the row-by-row update flow.
    public virtual async Task<IGenericResult> SetDefaultTenant(
        Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Step 1: load all rows for this user so we can verify membership and update per-row.
        var queryCommand = new QueryCommandBuilder<UserTenantConfiguration>(
                DataStoreName, PathName, "UserTenants")
            .Where(nameof(UserTenantConfiguration.UserId), userId)
            .Where(nameof(UserTenantConfiguration.IsCurrent), true)
            .Where(nameof(UserTenantConfiguration.IsDeleted), false)
            .Build();

        var queryResult = await Execute<IEnumerable<UserTenantConfiguration>>(queryCommand, cancellationToken).ConfigureAwait(false);
        if (!queryResult.IsSuccess)
        {
            UserConfigurationProviderLog.SetDefaultFailed(_logger, tenantId, userId);
            return queryResult.Messages.Any()
                ? (IGenericResult)queryResult
                : GenericResult.Failure(UserConfigurationProviderLog.SetDefaultFailed(_logger, tenantId, userId));
        }

        var allRows = queryResult.Value?.ToList() ?? new List<UserTenantConfiguration>();
        var targetRow = allRows.FirstOrDefault(r => r.TenantId == tenantId);
        if (targetRow is null)
            return GenericResult.Failure(UserConfigurationProviderLog.SetDefaultNotMember(_logger, userId, tenantId));

        // Step 2: update each row that needs its IsDefault flag changed.
        foreach (var row in allRows)
        {
            var shouldBeDefault = row.TenantId == tenantId;
            if (row.IsDefault == shouldBeDefault)
                continue;

            var updateCommand = CmdBuilders.Update.In<UserTenantConfiguration>("UserTenants")
                .DataStore(DataStoreName).Path(PathName)
                .Where(nameof(UserTenantConfiguration.Id), row.Id)
                .Value(new UserTenantConfiguration
                {
                    Id = row.Id,
                    UserId = row.UserId,
                    TenantId = row.TenantId,
                    IsDefault = shouldBeDefault,
                    IsCurrent = row.IsCurrent,
                    IsDeleted = row.IsDeleted,
                });

            var updateResult = await Execute<int>(updateCommand, cancellationToken).ConfigureAwait(false);
            if (!updateResult.IsSuccess)
                return updateResult.Messages.Any()
                    ? (IGenericResult)updateResult
                    : GenericResult.Failure(UserConfigurationProviderLog.SetDefaultFailed(_logger, tenantId, userId));
        }

        return GenericResult.Success();
    }
#pragma warning restore MA0051
}
