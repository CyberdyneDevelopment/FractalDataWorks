using System.Collections.Generic;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// A role an organization defines for its own members, as callers see it (FDW-1593).
/// </summary>
/// <param name="Name">The name people see; unique within the organization.</param>
/// <param name="Description">What the role is for; <see langword="null"/> when it states none.</param>
/// <param name="IsBuiltIn">Whether the organization was made with the role, which cannot be deleted.</param>
/// <param name="Permissions">The permissions the role carries; always at least one.</param>
public sealed record OrganizationRoleView(string Name, string? Description, bool IsBuiltIn, IReadOnlyList<string> Permissions);
