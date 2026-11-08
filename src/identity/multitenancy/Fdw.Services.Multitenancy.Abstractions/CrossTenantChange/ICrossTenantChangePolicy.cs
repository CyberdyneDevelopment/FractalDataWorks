using System;
using Fdw.Results;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Decides whether the caller may change an object that belongs to another tenant (FDW-4459).
/// </summary>
public interface ICrossTenantChangePolicy
{
    /// <summary>Decides whether a change is allowed.</summary>
    /// <param name="changeName">The setting that governs the kind of change: a field of <see cref="CrossTenantChangeOption"/>.</param>
    /// <param name="ownerTenantId">The tenant the object belongs to.</param>
    /// <param name="actorTenantId">The caller's active tenant; null when the caller has none.</param>
    /// <returns>Success when the change is allowed; a Forbidden failure that names the setting when it is not; another failure when the setting is declared nowhere.</returns>
    IGenericResult Permit(string changeName, Guid ownerTenantId, Guid? actorTenantId);
}
