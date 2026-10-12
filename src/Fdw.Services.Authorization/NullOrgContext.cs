using System;
using Fdw.Services.Multitenancy.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>
/// Null-object org context — used when org resolution is not wired in the DI container.
/// The org tier contributes zero grants.
/// </summary>
internal sealed class NullOrgContext : IOrgContext
{
    /// <summary>Gets the singleton instance.</summary>
    public static readonly NullOrgContext Instance = new();

    /// <inheritdoc />
    public OrganizationConfiguration? CurrentOrg => null;

    /// <inheritdoc />
    public Guid? OrgId => null;

    /// <inheritdoc />
    public bool HasOrg => false;
}
