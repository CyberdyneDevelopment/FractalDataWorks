using Fdw.Web.Http.Abstractions.Policies;
using Fdw.Web.Http.Abstractions.Security;

namespace Fdw.Web.RestEndpoints.Configuration;

/// <summary>
/// Default settings for a specific endpoint type.
/// Following TypeCollection pattern: store names, retrieve objects via GetByName(string).
/// </summary>
public record EndpointDefaultSettings(
    string SecurityMethodName,
    string RateLimitPolicyName,
    int TimeoutMs,
    long MaxBodySize,
    bool RequireAuthentication,
    string[] AllowedRoles)
{
    /// <summary>
    /// Gets the security method by name.
    /// </summary>
    public ISecurityMethod? SecurityMethod => SecurityMethods.ByName(SecurityMethodName);

    /// <summary>
    /// Gets the rate limit policy by name.
    /// </summary>
    public IRateLimitPolicy? RateLimitPolicy => RateLimitPolicies.ByName(RateLimitPolicyName);
};