using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;

namespace Fdw.Services.Users.Services;

/// <summary>
/// Makes an organization together with its starting roles and its first administrator, all or nothing (FDW-1758).
/// </summary>
/// <remarks>
/// The organization is made, given a copy of the roles of another organization of the same tenant, and the first administrator is given
/// one of those roles. If any step after the organization was made fails, what was made is removed again, so no organization is left
/// without roles or without an administrator.
/// </remarks>
public interface IOrganizationFounding
{
    /// <summary>Makes an organization with its starting roles and first administrator.</summary>
    /// <param name="organization">What the organization is.</param>
    /// <param name="copyRolesFrom">The organization of the same tenant whose roles the new one starts with.</param>
    /// <param name="administratorId">The person who becomes its first administrator; they must have an account and belong to the tenant.</param>
    /// <param name="roleName">The role, among those copied, the person holds.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    /// <returns>The new organization's id, or the reason nothing was made.</returns>
    Task<IGenericResult<Guid>> Found(NewOrganization organization, Guid copyRolesFrom, Guid administratorId, string roleName, CancellationToken cancellationToken = default);
}
