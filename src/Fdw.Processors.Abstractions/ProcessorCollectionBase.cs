using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Processors;

/// <summary>
/// Base class for processor TypeCollections.
/// Use with the <c>[TypeCollection]</c> attribute for source generation.
/// </summary>
/// <typeparam name="TBase">The processor base class that collection items derive from.</typeparam>
/// <typeparam name="TInterface">The processor interface for return types.</typeparam>
/// <remarks>
/// <para>
/// This class inherits from <see cref="TypeCollectionBase{TBase, TInterface}"/> to integrate
/// with the TypeCollection source generator. The generator creates:
/// <list type="bullet">
/// <item>Static properties for each <c>[TypeOption]</c> processor</item>
/// <item><c>ById(int)</c> - O(1) lookup by processor ID</item>
/// <item><c>ByName(string)</c> - O(1) lookup by processor name</item>
/// <item><c>All()</c> - Returns all registered processors</item>
/// <item><c>NotFound()</c> - Returns the Empty sentinel processor</item>
/// </list>
/// </para>
/// <para>
/// Domain-specific collections should inherit from this class and apply
/// the <c>[TypeCollection]</c> attribute. Example:
/// <code>
/// [TypeCollection(typeof(MyProcessorBase), typeof(IMyProcessor), typeof(MyProcessors))]
/// public abstract partial class MyProcessors 
///     : ProcessorCollectionBase&lt;MyProcessorBase, IMyProcessor&gt;
/// {
/// }
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Look up a processor by name
/// var processor = MyProcessors.ByName("ProcessorName");
/// if (processor.IsEmpty)
/// {
///     // Handle unknown processor
/// }
/// 
/// // Iterate all processors
/// foreach (var p in MyProcessors.All())
/// {
///     Console.WriteLine(p.Name);
/// }
/// </code>
/// </example>
[ExcludeFromCodeCoverage]
public abstract class ProcessorCollectionBase<TBase, TInterface>
    : TypeCollectionBase<TBase, TInterface>
    where TBase : class, TInterface
    where TInterface : class
{
}
