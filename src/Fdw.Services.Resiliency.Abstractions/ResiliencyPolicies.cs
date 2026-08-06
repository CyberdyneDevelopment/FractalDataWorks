using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Resiliency.Abstractions;

/// <summary>
/// Collection of all resiliency policy types.
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
/// var policy = ResiliencyPolicies.ByName("Database");
/// var delay = policy.InitialDelay;
/// </code>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ResiliencyPolicyBase), typeof(IResiliencyPolicy), typeof(ResiliencyPolicies))]
public partial class ResiliencyPolicies : TypeCollectionBase<ResiliencyPolicyBase, IResiliencyPolicy>
{
}
