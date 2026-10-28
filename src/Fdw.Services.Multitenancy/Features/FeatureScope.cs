using Fdw.Services.Authorization.Abstractions;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// The rights the feature switches ask for the features of an organization (FDW-2474) or of a whole company (FDW-2478).
/// </summary>
/// <remarks>
/// An organization is judged by <see cref="IOrganizationScope"/>, so one of another company and one the store does not hold are refused in
/// the same way (FDW-2887). A whole company is judged at its own scope, with no organization, by <see cref="IScopeAuthority"/>: the caller
/// holds the right in that company, which they act in. A caller who does not act in it is refused whether or not it exists.
/// </remarks>
internal static class FeatureScope
{
    internal const string ReadRight = "organizations:read";
    internal const string WriteRight = "organizations:write";
    internal const string TenantReadRight = "tenants:read";
    internal const string TenantWriteRight = "tenants:write";
}
