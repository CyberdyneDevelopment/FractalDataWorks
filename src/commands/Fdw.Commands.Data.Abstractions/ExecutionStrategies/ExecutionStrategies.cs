using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// TypeCollection for composite command execution strategies.
/// Source generator will create static properties for each strategy with [TypeOption] attribute.
/// </summary>
/// <remarks>
/// <para>
/// This collection provides compile-time discovery of all execution strategy types.
/// No switch statements needed - strategies know their own execution characteristics!
/// </para>
/// <para>
/// Example generated properties:
/// <list type="bullet">
/// <item>ExecutionStrategies.Sequential - Execute commands one after another</item>
/// <item>ExecutionStrategies.Parallel - Execute commands in parallel where possible</item>
/// <item>ExecutionStrategies.SequentialStopOnFailure - Stop on first failure</item>
/// <item>ExecutionStrategies.SequentialContinueOnFailure - Continue even if commands fail</item>
/// </list>
/// </para>
/// <para>
/// Usage eliminates switch statements:
/// <code>
/// var composite = new CompositeCommand {
///     Commands = [cmd1, cmd2, cmd3],
///     ExecutionStrategy = ExecutionStrategies.Sequential  // Type-safe!
/// };
///
/// // No switch - just property access!
/// if (composite.ExecutionStrategy.StopOnFailure) {
///     // Handle stop-on-failure logic
/// }
/// </code>
/// </para>
/// </remarks>
[TypeCollection(typeof(ExecutionStrategyBase), typeof(ExecutionStrategyBase), typeof(ExecutionStrategies))]
[ExcludeFromCodeCoverage]
public abstract partial class ExecutionStrategies : TypeCollectionBase<ExecutionStrategyBase, ExecutionStrategyBase>
{
    // Source generator will create:
    // - Static constructor
    // - Static properties for each [TypeOption] strategy
    // - All() method
    // - ByName() method
    // - ById() method
}
