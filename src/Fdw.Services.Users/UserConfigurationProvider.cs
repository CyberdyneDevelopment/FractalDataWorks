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
using Fdw.Services.Users.Models;
using Fdw.Services.Users.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Users;

/// <summary>
/// Domain configuration provider for users. Sole owner of <c>usr.Users</c> gateway access.
/// Thin wrapper over <see cref="DefaultConfigurationProvider{TConfig,TCommand}"/> with
/// by-id, by-username, and CRUD convenience methods.
/// </summary>
/// <remarks>
/// All reads and writes go through <see cref="IConfigurationGateway"/>. No <see cref="Fdw.Services.Data.Abstractions.IDataGateway"/>
/// usage — usr.Users is ConfigurationDb data, and the schema-built ConfigurationDb store has no ConnectionId,
/// so routing through IDataGateway produces "DataStore 'ConfigurationDb' has no ConnectionId".
/// </remarks>
public class UserConfigurationProvider : DefaultConfigurationProvider<UserConfiguration, UserConfigurationCommand>
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="UserConfigurationProvider"/> class.</summary>
    public UserConfigurationProvider(
        ILogger<UserConfigurationProvider>? logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "usr")
        : base(logger ?? NullLogger<UserConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName)
    {
        _logger = logger ?? NullLogger<UserConfigurationProvider>.Instance;
    }

    /// <summary>
    /// Gets a user by their unique identifier.
    /// </summary>
    // Why: virtual allows Moq to override in unit tests without a real IOptionsMonitor or gateway.
    public virtual async Task<IGenericResult<UserConfiguration?>> GetUser(
        Guid userId, CancellationToken cancellationToken = default)
    {
        UserConfigurationProviderLog.LoadByIdTrace(_logger, userId);
        var result = await Get(userId, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? GenericResult<UserConfiguration?>.Success(result.Value)
            : result.ToNewResult<UserConfiguration?>();
    }

    /// <summary>
    /// Gets a user by their username.
    /// </summary>
    // Why: virtual — same test-isolation rationale as GetUser(Guid).
    // Why: Does NOT delegate to base.Get(string) — that method filters on the [Name] column via
    // ConfigurationCommandBase.NameColumn. usr.Users has no [Name] column; the natural-key column
    // is [Username]. An explicit QueryCommandBuilder<UserConfiguration> with .Where("Username", ...)
    // is the only correct path. This mirrors the pre-provider SqlUserService username query.
    public virtual async Task<IGenericResult<UserConfiguration?>> GetUser(
        string username, CancellationToken cancellationToken = default)
    {
        UserConfigurationProviderLog.LoadByUsernameTrace(_logger, username);
        var cmd = new QueryCommandBuilder<UserConfiguration>(DataStoreName, PathName, "Users")
            .Where("Username", username)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Build();
        var result = await Gateway.Execute<IEnumerable<UserConfiguration>>(cmd, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            return GenericResult<UserConfiguration?>.Failure(
                UserConfigurationProviderLog.GatewayQueryFailed(_logger));
        return GenericResult<UserConfiguration?>.Success(result.Value?.FirstOrDefault());
    }

    /// <summary>
    /// Resolves a user from a route segment that may carry either a Guid id or a username (API-66).
    /// </summary>
    /// <param name="idOrName">The route value: a Guid id, or a username.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved user, or a failure carrying <c>UserNotFound</c> when no user matches.</returns>
    // Why: THE single id-or-name resolution point for every user-scoped route. This resolution used to
    // be copy-pasted inline into each endpoint, and the copies drifted — the user-role endpoints ended
    // up binding a raw Guid on assign but a username-only lookup on revoke/get-roles, so a client that
    // sent an id could add a role but never remove it (the username query silently matched nothing).
    // One resolver means a new user-scoped endpoint cannot reinvent that inconsistency.
    // Why: fails loud with UserNotFound rather than returning a null Value on a miss — callers get a
    // failed result they must handle, not a null they can accidentally treat as "no user, carry on".
    public virtual async Task<IGenericResult<UserConfiguration>> ResolveUser(
        string idOrName, CancellationToken cancellationToken = default)
    {
        var result = Guid.TryParse(idOrName, out var userId)
            ? await GetUser(userId, cancellationToken).ConfigureAwait(false)
            : await GetUser(idOrName, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
            return result.ToNewResult<UserConfiguration>();

        return result.Value is null
            ? GenericResult<UserConfiguration>.Failure(UserResultCodes.ByName("UserNotFound"))
            : GenericResult<UserConfiguration>.Success(result.Value);
    }

    /// <summary>
    /// Gets all users.
    /// </summary>
    // Why: virtual — same test-isolation rationale as GetUser(Guid).
    public virtual async Task<IGenericResult<IReadOnlyList<UserConfiguration>>> GetAllUsers(
        CancellationToken cancellationToken = default)
    {
        var result = await Get(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            UserConfigurationProviderLog.LoadFailed(_logger);
            return result.ToNewResult<IReadOnlyList<UserConfiguration>>();
        }

        UserConfigurationProviderLog.LoadAllLoaded(_logger, result.Value.Count);
        return GenericResult<IReadOnlyList<UserConfiguration>>.Success(result.Value);
    }

    /// <summary>
    /// Creates a new user record and returns the new user's ID on success.
    /// Does NOT grant tenant membership — callers must call
    /// <see cref="UserTenantConfigurationProvider.GrantTenantAccess"/> after this returns success.
    /// </summary>
    // Why: tenant membership grant is the caller's responsibility so this method stays single-purpose
    // and testable without a tenant provider dependency. Callers (e.g. CreateUserEndpointBase)
    // orchestrate provider calls in order.
    // Why: virtual — same test-isolation rationale as GetUser.
    public virtual async Task<IGenericResult<Guid>> CreateUser(
        string username,
        string? email,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        UserConfigurationProviderLog.CreateUserTrace(_logger, username);

        var existing = await GetUser(username, cancellationToken).ConfigureAwait(false);
        if (existing.IsSuccess && existing.Value is not null)
            return GenericResult<Guid>.Failure(
                UserResultCodes.ByName("UserAlreadyExists"),
                ResultDetails.Create("username", username));

        var userId = Guid.CreateVersion7();
        var record = new UserConfiguration
        {
            Id = userId,
            Username = username,
            Email = email,
            IsActive = true,
            IsCurrent = true,
            IsDeleted = false,
            TenantId = tenantId,
        };

        var saveResult = await Save(record, cancellationToken).ConfigureAwait(false);
        if (!saveResult.IsSuccess)
        {
            return saveResult.Messages.Any()
                ? saveResult.ToNewResult<Guid>()
                : GenericResult<Guid>.Failure(
                    UserLog.UserCreateFailed(_logger, new InvalidOperationException("Insert failed"), username));
        }

        UserLog.UserCreated(_logger, username, userId);
        return GenericResult<Guid>.Success(userId);
    }

    /// <summary>
    /// Updates an existing user record from the <see cref="IUser"/> contract.
    /// </summary>
    // Why: virtual — same test-isolation rationale as GetUser.
    public virtual async Task<IGenericResult> UpdateUser(
        IUser user, CancellationToken cancellationToken = default)
    {
        UserConfigurationProviderLog.UpdateUserTrace(_logger, user.Id);

        var existing = await GetUser(user.Id, cancellationToken).ConfigureAwait(false);
        if (!existing.IsSuccess || existing.Value is null)
        {
            return existing.Messages.Any()
                ? (IGenericResult)existing
                : GenericResult.Failure(
                    UserLog.UserUpdateFailed(_logger, new InvalidOperationException("User not found"), user.Id));
        }

        var record = existing.Value;
        record.Username = user.Username;
        record.Email = user.Email;
        record.IsActive = user.IsActive;
        record.LastLoginAt = user.LastLoginAt;

        var saveResult = await Save(record, cancellationToken).ConfigureAwait(false);
        if (!saveResult.IsSuccess)
        {
            return saveResult.Messages.Any()
                ? (IGenericResult)saveResult
                : GenericResult.Failure(
                    UserLog.UserUpdateFailed(_logger, new InvalidOperationException("Update failed"), user.Id));
        }

        return GenericResult.Success();
    }

    /// <summary>
    /// Soft-deletes a user by setting <c>IsActive=false</c>, <c>IsCurrent=false</c>, and <c>IsDeleted=true</c>.
    /// </summary>
    // Why: virtual — same test-isolation rationale as GetUser.
    public virtual async Task<IGenericResult> DeleteUser(
        Guid userId, CancellationToken cancellationToken = default)
    {
        UserConfigurationProviderLog.DeleteUserTrace(_logger, userId);

        var existing = await GetUser(userId, cancellationToken).ConfigureAwait(false);
        if (!existing.IsSuccess || existing.Value is null)
        {
            return existing.Messages.Any()
                ? (IGenericResult)existing
                : GenericResult.Failure(
                    UserLog.UserDeleteFailed(_logger, new InvalidOperationException("User not found"), userId));
        }

        var record = existing.Value;
        record.IsActive = false;
        record.IsCurrent = false;
        record.IsDeleted = true;

        var saveResult = await Save(record, cancellationToken).ConfigureAwait(false);
        if (!saveResult.IsSuccess)
        {
            return saveResult.Messages.Any()
                ? (IGenericResult)saveResult
                : GenericResult.Failure(
                    UserLog.UserDeleteFailed(_logger, new InvalidOperationException("Delete failed"), userId));
        }

        return GenericResult.Success();
    }

    /// <summary>
    /// Updates the <c>LastLoginAt</c> timestamp for a user to the current UTC time.
    /// </summary>
    // Why: virtual — same test-isolation rationale as GetUser.
    public virtual async Task<IGenericResult> UpdateLastLogin(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await GetUser(userId, cancellationToken).ConfigureAwait(false);
        if (!existing.IsSuccess || existing.Value is null)
        {
            return existing.Messages.Any()
                ? (IGenericResult)existing
                : GenericResult.Failure(
                    UserLog.LastLoginUpdateFailed(_logger, new InvalidOperationException("User not found"), userId));
        }

        var record = existing.Value;
        record.LastLoginAt = DateTimeOffset.UtcNow;

        var saveResult = await Save(record, cancellationToken).ConfigureAwait(false);
        if (!saveResult.IsSuccess)
        {
            return saveResult.Messages.Any()
                ? (IGenericResult)saveResult
                : GenericResult.Failure(
                    UserLog.LastLoginUpdateFailed(_logger, new InvalidOperationException("Update failed"), userId));
        }

        return GenericResult.Success();
    }
}
