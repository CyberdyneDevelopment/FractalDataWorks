using System;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Request-scoped org context implementation.
/// Registered as a scoped service; <c>OrgResolutionMiddleware</c> calls <see cref="SetOrg"/>
/// after resolving the org from JWT <c>org_id</c>, <c>X-Org-Id</c> header, or the tenant's
/// default org.
/// </summary>
public sealed class MutableOrgContext : IMutableOrgContext
{
    private OrganizationConfiguration? _currentOrg;

    /// <inheritdoc />
    public OrganizationConfiguration? CurrentOrg => _currentOrg;

    /// <inheritdoc />
    public Guid? OrgId => _currentOrg?.Id;

    /// <inheritdoc />
    public bool HasOrg => _currentOrg != null;

    /// <inheritdoc />
    public void SetOrg(OrganizationConfiguration org)
    {
        _currentOrg = org ?? throw new ArgumentNullException(nameof(org));
    }

    /// <inheritdoc />
    public void Clear()
    {
        _currentOrg = null;
    }
}
