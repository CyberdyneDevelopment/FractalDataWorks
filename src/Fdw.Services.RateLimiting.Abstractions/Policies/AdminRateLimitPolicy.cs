using Fdw.Collections.Attributes;

namespace Fdw.Services.RateLimiting.Abstractions.Policies;

/// <summary>
/// Rate limit policy for administrative API access.
/// Held to the highest limits, for internal tools, administrative dashboards and system integration services.
/// </summary>
/// <remarks>
/// Its numbers are the <c>RateLimit.Admin.*</c> settings, seeded in each host's baseline runtime script.
/// </remarks>
[TypeOption(typeof(RateLimitPolicies), "Admin", RestrictToCurrentCompilation = true)]
public sealed class AdminRateLimitPolicy : RateLimitPolicyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdminRateLimitPolicy"/> class.
    /// </summary>
    public AdminRateLimitPolicy() : base("Admin")
    {
    }
}
