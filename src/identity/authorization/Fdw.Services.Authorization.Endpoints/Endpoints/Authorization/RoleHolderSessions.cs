using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Ends the live sessions of everyone who holds a role, after something changed what that role grants.
/// </summary>
/// <remarks>
/// A token carries the permissions it was issued with until it expires. Revoking one user's role
/// already ends that user's sessions (FDW-875). Deleting the role, or replacing its permissions,
/// changes what every holder may do in the same way, so their sessions end too. Each holder's next
/// sign-in reads the change. Holders are the current, non-deleted assignments of the role.
/// </remarks>
internal static class RoleHolderSessions
{
    /// <summary>Ends the sessions of every current holder of the role.</summary>
    public static async Task<IGenericResult> End(
        IUserRoleConfigurationProvider userRoles,
        ITokenRevocationStore revocations,
        Guid roleId,
        string roleName,
        ILogger logger,
        CancellationToken ct)
    {
        var holders = await Holders(userRoles, roleId, roleName, logger, ct).ConfigureAwait(false);
        if (holders.IsFailure) return holders;

        return await End(revocations, holders.Value!, roleName, logger, ct).ConfigureAwait(false);
    }

    /// <summary>Reads who holds the role now: the person (or service identity) of each current, non-deleted assignment, each once.</summary>
    /// <remarks>
    /// Read apart from <see cref="End(ITokenRevocationStore, IReadOnlyList{string}, string, ILogger, CancellationToken)"/> for a change that
    /// removes the assignments themselves: deleting a role removes them with it (FDW-5222), so its holders are read before the delete.
    /// </remarks>
    public static async Task<IGenericResult<IReadOnlyList<string>>> Holders(
        IUserRoleConfigurationProvider userRoles,
        Guid roleId,
        string roleName,
        ILogger logger,
        CancellationToken ct)
    {
        var holders = await userRoles
            .Find<IUserRoleImplementationConfiguration>(ur => ur.RoleId == roleId && ur.IsCurrent && !ur.IsDeleted, ct)
            .ConfigureAwait(false);
        if (holders.IsFailure || holders.Value is null)
            return GenericResult<IReadOnlyList<string>>.Failure(AuthorizationEndpointLog.RoleHoldersUnreadable(logger, roleName));

        return GenericResult<IReadOnlyList<string>>.Success(
            [.. holders.Value.Select(h => h.UserId).Distinct(StringComparer.OrdinalIgnoreCase)]);
    }

    /// <summary>Ends the sessions of the holders already read.</summary>
    public static async Task<IGenericResult> End(
        ITokenRevocationStore revocations,
        IReadOnlyList<string> holderIds,
        string roleName,
        ILogger logger,
        CancellationToken ct)
    {
        var revokedAt = DateTimeOffset.UtcNow;

        // Every holder is attempted, whatever happened to the one before: stopping at the first that cannot be
        // ended would leave every later holder carrying the old grants, after the change itself has committed.
        IGenericMessage? firstFailure = null;
        var endedCount = 0;
        foreach (var holderId in holderIds)
        {
            // A holder that is not a user's Guid is a service identity. It is never issued a locally signed
            // token or a session, so it has nothing to end; the next one is still ended.
            if (!Guid.TryParse(holderId, out var userId))
            {
                AuthorizationEndpointLog.RoleHolderHasNoSession(logger, roleName, holderId);
                continue;
            }

            var ended = await revocations.RevokeAll(userId, revokedAt, ct).ConfigureAwait(false);
            if (ended.IsFailure)
            {
                firstFailure ??= AuthorizationEndpointLog.RoleHolderSessionsNotEnded(logger, roleName, holderId);
                continue;
            }

            endedCount++;
        }

        if (firstFailure is not null)
            return GenericResult.Failure(firstFailure);

        AuthorizationEndpointLog.RoleHolderSessionsEnded(logger, roleName, endedCount);
        return GenericResult.Success();
    }
}
