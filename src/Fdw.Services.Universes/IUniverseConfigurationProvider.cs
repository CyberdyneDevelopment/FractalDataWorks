using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Universes;

/// <summary>
/// Reads and writes universe configurations.
/// </summary>
/// <remarks>
/// Three Get overloads and nothing else: by name, by id, and all of them. A caller that wants
/// one universe's members reads the universe and walks it, rather than asking a provider for a
/// filtered slice — the aggregate the provider returns is already navigable.
/// </remarks>
public interface IUniverseConfigurationProvider
{
    /// <summary>Gets a universe by name, with its members, resources and relationships.</summary>
    /// <param name="name">The universe name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<UniverseConfiguration>> Get(string name, CancellationToken cancellationToken = default);

    /// <summary>Gets a universe by its logical identifier.</summary>
    /// <param name="id">The universe's logical identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<UniverseConfiguration>> Get(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets every universe visible to the caller.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<IReadOnlyList<UniverseConfiguration>>> Get(CancellationToken cancellationToken = default);

    /// <summary>Creates or updates a universe and its children.</summary>
    /// <param name="record">The universe to persist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<UniverseConfiguration>> Save(UniverseConfiguration record, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a universe.</summary>
    /// <param name="id">The universe's logical identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult> Delete(Guid id, CancellationToken cancellationToken = default);

    // The narrow writes below exist because Save cascades unconditionally: using it to change one
    // child stamps every other child's audit columns and overwrites anything written in between.
    // Each of these touches only the row it names.

    /// <summary>Changes one member's role, leaving every other row alone.</summary>
    /// <param name="universeId">The universe the member belongs to.</param>
    /// <param name="memberId">The membership row's logical identifier.</param>
    /// <param name="memberRole">The new role. Rejected if not a registered UniverseMemberRoles option.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<UniverseMemberConfiguration>> SetMemberRole(
        Guid universeId, Guid memberId, string memberRole, CancellationToken cancellationToken = default);

    /// <summary>Adds one member.</summary>
    /// <param name="universeId">The universe to add to.</param>
    /// <param name="member">The membership row.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<UniverseMemberConfiguration>> AddMember(
        Guid universeId, UniverseMemberConfiguration member, CancellationToken cancellationToken = default);

    /// <summary>Removes one member.</summary>
    /// <param name="memberId">The membership row's logical identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult> RemoveMember(Guid memberId, CancellationToken cancellationToken = default);

    /// <summary>Attaches one resource.</summary>
    /// <param name="universeId">The universe to attach to.</param>
    /// <param name="resource">The attachment row.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<UniverseResourceConfiguration>> AttachResource(
        Guid universeId, UniverseResourceConfiguration resource, CancellationToken cancellationToken = default);

    /// <summary>Detaches one resource.</summary>
    /// <param name="resourceId">The attachment row's logical identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult> DetachResource(Guid resourceId, CancellationToken cancellationToken = default);

    /// <summary>Sets or clears the fields a declared relationship joins on.</summary>
    /// <param name="universeId">The universe the relationship belongs to.</param>
    /// <param name="relationshipId">The relationship's logical identifier.</param>
    /// <param name="leftFieldId">The left field, or null to leave the key undefined.</param>
    /// <param name="rightFieldId">The right field, or null to leave the key undefined.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<UniverseRelationshipConfiguration>> SetRelationshipJoinKey(
        Guid universeId, Guid relationshipId, Guid? leftFieldId, Guid? rightFieldId,
        CancellationToken cancellationToken = default);
}
