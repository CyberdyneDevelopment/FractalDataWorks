using Fdw.Collections.Attributes;

namespace Fdw.Services.RateLimiting.Abstractions.Policies;

/// <summary>
/// Rate limit policy for authenticated API users.
/// Held to higher limits than standard access as a reward for user registration
/// and to support typical authenticated application workflows.
/// </summary>
/// <remarks>
/// Its numbers are the <c>RateLimit.Authenticated.*</c> settings, seeded in each host's baseline runtime script.
/// </remarks>
[TypeOption(typeof(RateLimitPolicies), "Authenticated", RestrictToCurrentCompilation = true)]
public sealed class AuthenticatedRateLimitPolicy : RateLimitPolicyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticatedRateLimitPolicy"/> class.
    /// </summary>
    public AuthenticatedRateLimitPolicy() : base("Authenticated")
    {
    }
}
