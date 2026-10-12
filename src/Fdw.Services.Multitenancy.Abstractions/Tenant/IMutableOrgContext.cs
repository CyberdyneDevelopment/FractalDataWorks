namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Mutable org context for setting the current organization during request processing.
/// Set by <c>OrgResolutionMiddleware</c> after tenant resolution.
/// </summary>
public interface IMutableOrgContext : IOrgContext
{
    /// <summary>
    /// Sets the current organization.
    /// </summary>
    void SetOrg(OrganizationConfiguration org);

    /// <summary>
    /// Clears the current org context.
    /// </summary>
    void Clear();
}
