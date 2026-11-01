using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// The one rule for who may give a role or define one: a caller can give or define only what they hold themselves, and only in
/// the tenant they act in.
/// </summary>
/// <remarks>
/// Why a rule and not a policy name: <c>users:write</c> says a caller may change roles, not which ones. Without this rule any
/// holder of it can give Admin. The rule compares the permissions a role carries with the permissions the caller holds
/// (<c>resource:*</c> and <c>*:*</c> held count for what they cover), so a caller never gives more than they have. The system
/// context is not limited by the rule: it acts for the platform itself.
/// </remarks>
public interface IGrantRule
{
    /// <summary>
    /// Says whether the caller may give these roles to a person in a tenant.
    /// </summary>
    /// <param name="roleNames">The roles to give.</param>
    /// <param name="tenantId">The tenant they are given in.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the caller may; otherwise the reason, as a result that names what the caller lacks.</returns>
    Task<IGenericResult> CanGive(IReadOnlyList<string> roleNames, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Says whether the caller may give these roles to the first administrator of a tenant that did not exist until now (FDW-1578).
    /// </summary>
    /// <param name="roleNames">The roles to give.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the caller holds every permission the roles carry; otherwise the reason, as a result that names what the caller lacks.</returns>
    /// <remarks>
    /// Why a second question: <see cref="CanGive"/> limits a caller to the tenant they act in, and a tenant being made is never that one.
    /// Only what the caller holds is compared. The caller of this method decides who may make a tenant at all, by the policy on the
    /// endpoint that makes one; nothing else should call it.
    /// </remarks>
    Task<IGenericResult> CanGiveInNewTenant(IReadOnlyList<string> roleNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Says whether the caller may put these permissions into a role of a tenant.
    /// </summary>
    /// <param name="permissionNames">The permissions the role is to carry.</param>
    /// <param name="tenantId">The tenant the role belongs to.</param>
    /// <returns>Success when the caller may; otherwise the reason, as a result that names what the caller lacks.</returns>
    IGenericResult CanDefine(IReadOnlyList<string> permissionNames, Guid tenantId);
}
