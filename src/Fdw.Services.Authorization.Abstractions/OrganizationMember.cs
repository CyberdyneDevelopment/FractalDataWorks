using System;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// A person in an organization and the one role they hold there (FDW-1593).
/// </summary>
/// <param name="UserId">The person.</param>
/// <param name="RoleName">The role they hold in the organization.</param>
public sealed record OrganizationMember(Guid UserId, string RoleName);
