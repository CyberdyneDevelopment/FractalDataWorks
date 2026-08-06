namespace Fdw.Web.RestEndpoints.Security;

/// <summary>
/// String constants for rate limit policy names that match the
/// <c>RateLimitPolicies</c> TypeCollection in <c>Services.RateLimiting.Abstractions</c>.
/// Use these in endpoint bases and middleware configuration to ensure consistent policy references.
/// </summary>
public static class RateLimitPolicyNames
{
    /// <summary>Standard rate limiting: 100 requests per minute. Used for anonymous/public endpoints.</summary>
    public const string Standard = "Standard";

    /// <summary>Authenticated rate limiting: 500 requests per minute. Used for authenticated endpoints.</summary>
    public const string Authenticated = "Authenticated";

    /// <summary>Premium rate limiting: 2000 requests per minute. Used for premium-tier endpoints.</summary>
    public const string Premium = "Premium";

    /// <summary>Admin rate limiting: 10000 requests per minute. Used for administrative endpoints.</summary>
    public const string Admin = "Admin";
}
