namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// What a change to a tenant states (FDW-1578). A part left <see langword="null"/> is not stated, so the tenant keeps what it has.
/// </summary>
/// <param name="Name">The new display name.</param>
/// <param name="IsActive">Whether the tenant is active: <see langword="false"/> disables it, <see langword="true"/> enables it again.</param>
/// <param name="ConnectionName">The new connection name.</param>
public sealed record TenantChange(string? Name, bool? IsActive, string? ConnectionName);
