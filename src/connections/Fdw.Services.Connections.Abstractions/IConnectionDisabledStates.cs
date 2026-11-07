using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Whether a connection is disabled, and the way to disable and enable one (FDW-3951).
/// </summary>
/// <remarks>
/// A disabled connection is refused wherever it is resolved, with the coded failure ConnectionDisabled. The state is
/// kept apart from the connection's definition: a threshold firing on the ETL host records it without saving the
/// connection, so it cannot race a person's edit, and an ordinary save of the connection cannot write a stale state back.
/// Every change is a new version of the state, so the history of disabling and enabling is kept.
/// </remarks>
public interface IConnectionDisabledStates
{
    /// <summary>Reads the state of one connection, from the store and not from a cache.</summary>
    /// <param name="connectionId">The Id of the connection's domain row.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The record when the connection is disabled, or a successful result carrying nothing when it is not.</returns>
    Task<IGenericResult<ConnectionDisabledRecord?>> Read(Guid connectionId, CancellationToken cancellationToken = default);

    /// <summary>Disables a connection and records why. The state is stamped with the connection's own tenant, read from its row, so that tenant's reads see it.</summary>
    /// <param name="connectionId">The Id of the connection's domain row.</param>
    /// <param name="requestingTenantId">The tenant on whose behalf the connection is disabled (the tenant of the threshold that fired).</param>
    /// <param name="requesterMayDisableOtherTenants">Whether the requesting tenant may disable a connection another tenant owns (a global-tenant rule, when the platform allows it). When it may not, only the connection's own tenant may disable it.</param>
    /// <param name="reason">Why the connection is disabled. Required.</param>
    /// <param name="disabledBy">Who or what disables it. Required.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The record of the disabled connection. A connection that is already disabled is left as it is and its record is
    /// returned: the first reason stands. A connection with no row the reader can see is a failure, and so is one another tenant owns: that is refused with a Forbidden failure and nothing is written.
    /// </returns>
    Task<IGenericResult<ConnectionDisabledRecord>> Disable(
        Guid connectionId, Guid requestingTenantId, bool requesterMayDisableOtherTenants, string reason, string disabledBy, CancellationToken cancellationToken = default);

    /// <summary>Enables a connection again and records why.</summary>
    /// <param name="connectionId">The Id of the connection's domain row.</param>
    /// <param name="reason">Why it is enabled again. Required.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success; also when the connection was not disabled, which leaves it as it is.</returns>
    Task<IGenericResult> Enable(Guid connectionId, string reason, CancellationToken cancellationToken = default);
}
