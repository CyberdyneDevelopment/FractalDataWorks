using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// Collection of all rate limit policy types.
/// Provides O(1) lookup by Id and Name through source-generated FrozenDictionary.
/// </summary>
/// <remarks>
/// <para>
/// This collection is populated by the source generator which discovers all types
/// decorated with <see cref="TypeOptionAttribute"/> that target this collection.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var policy = RateLimitPolicies.ByName("Standard");
/// var requestsPerWindow = policy.RequestsPerWindow;
/// var window = policy.Window;
/// </code>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(RateLimitPolicyBase), typeof(IRateLimitPolicy), typeof(RateLimitPolicies))]
public partial class RateLimitPolicies : TypeCollectionBase<RateLimitPolicyBase, IRateLimitPolicy>
{
}
