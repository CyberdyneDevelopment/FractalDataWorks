using Fdw.Collections;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// Base class for rate limit policy implementations: a tier of callers, known by its name. What the tier is held to is read
/// from settings for each request (<see cref="RateLimitAllowance"/>).
/// </summary>
public abstract class RateLimitPolicyBase : TypeOptionBase<IRateLimitPolicy>, ITypeOption<int, RateLimitPolicyBase>, IRateLimitPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitPolicyBase"/> class.
    /// </summary>
    /// <param name="name">Name of the policy.</param>
    protected RateLimitPolicyBase(string name) : base(name)
    {
    }
}
