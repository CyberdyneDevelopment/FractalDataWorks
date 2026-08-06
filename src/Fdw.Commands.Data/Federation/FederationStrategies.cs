using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Collection of federation strategies for data operations.
/// </summary>
/// <remarks>
/// <para>
/// Source generator discovers all types marked with [TypeOption(typeof(FederationStrategies), ...)].
/// Source generator also creates static properties for each type: Sequential, Parallel, Optimized.
/// </para>
/// <para>
/// Usage:
/// <code>
/// // Access via static properties
/// var parallel = FederationStrategies.Parallel;
/// var sequential = FederationStrategies.Sequential;
///
/// // Or lookup by name/id
/// var strategy = FederationStrategies.ByName("Parallel");
/// var strategy = FederationStrategies.ById(2);
///
/// // Get all strategies
/// var all = FederationStrategies.All();
/// </code>
/// </para>
/// </remarks>
[TypeCollection(typeof(FederationStrategyBase), typeof(IFederationStrategy), typeof(FederationStrategies))]
[ExcludeFromCodeCoverage]
public abstract partial class FederationStrategies : TypeCollectionBase<FederationStrategyBase, IFederationStrategy>
{
    // Source generator will implement:
    // - public static IFederationStrategy Sequential { get; }
    // - public static IFederationStrategy Parallel { get; }
    // - public static IFederationStrategy Optimized { get; }
    // - public static FrozenDictionary<int, IFederationStrategy> All()
    // - public static IFederationStrategy ById(int id)
    // - public static IFederationStrategy ByName(string name)
    // - public static void Register(IFederationStrategy type)
    // - public static IFederationStrategy Empty
}
