namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// What a change to an organization states (FDW-1758). A part left <see langword="null"/> is not stated, so the organization keeps what it has.
/// </summary>
/// <param name="Name">The new display name.</param>
/// <param name="IsActive">Whether the organization is active: <see langword="false"/> disables it, <see langword="true"/> enables it again.</param>
public sealed record OrganizationChange(string? Name, bool? IsActive);
