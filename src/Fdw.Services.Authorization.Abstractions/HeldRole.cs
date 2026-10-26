using System;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>A role a person holds, as an assignment states it (FDW-2668).</summary>
/// <param name="RoleId">The id of the role.</param>
/// <param name="TenantId">The tenant the role is held in; a role is held per tenant.</param>
/// <param name="ExpiresAt">When the assignment stops counting; none when it does not expire.</param>
public sealed record HeldRole(Guid RoleId, Guid TenantId, DateTimeOffset? ExpiresAt);
