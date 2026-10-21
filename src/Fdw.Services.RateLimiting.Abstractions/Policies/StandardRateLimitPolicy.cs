using Fdw.Collections.Attributes;

namespace Fdw.Services.RateLimiting.Abstractions.Policies;

/// <summary>
/// Rate limit policy for standard unauthenticated API access.
/// Held to conservative limits to protect the API from abuse while allowing
/// reasonable usage for anonymous or unpaid users.
/// </summary>
/// <remarks>
/// Its numbers are the <c>RateLimit.Standard.*</c> settings, seeded in each host's baseline runtime script.
/// </remarks>
[TypeOption(typeof(RateLimitPolicies), "Standard", RestrictToCurrentCompilation = true)]
public sealed class StandardRateLimitPolicy : RateLimitPolicyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StandardRateLimitPolicy"/> class.
    /// </summary>
    public StandardRateLimitPolicy() : base("Standard")
    {
    }
}
