namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// What giving a person a role in an organization changed (FDW-1593).
/// </summary>
/// <param name="PreviousRole">The role they held there before; <see langword="null"/> when they were not in the organization.</param>
/// <param name="Role">The role they hold there now.</param>
public sealed record OrganizationMemberChange(string? PreviousRole, string Role);
