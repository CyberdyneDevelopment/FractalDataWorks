using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>
/// MutableTypeCollection for data layouts.
/// Source generator will create static properties for each layout with [TypeOption] attribute.
/// </summary>
/// <remarks>
/// <para>
/// This collection provides compile-time discovery of all data layout types.
/// No switch statements needed - layouts know their own characteristics!
/// </para>
/// <para>
/// Example generated properties:
/// <list type="bullet">
/// <item>DataLayouts.Tabular - Flat rows and columns (SQL table, CSV, Excel)</item>
/// <item>DataLayouts.Hierarchical - Nested parent-child structure (JSON, XML)</item>
/// <item>DataLayouts.Document - Single complex object (MongoDB document, config)</item>
/// <item>DataLayouts.KeyValue - Key-value pairs (Redis, config sections)</item>
/// <item>DataLayouts.Graph - Nodes and edges (Neo4j, relationships)</item>
/// </list>
/// </para>
/// <para>
/// Usage eliminates switch statements:
/// <code>
/// var schema = new SchemaDefinition {
///     Name = "Customer",
///     Layout = DataLayouts.Tabular,  // Type-safe!
///     Properties = customerProperties
/// };
///
/// // No switch - just property access!
/// if (schema.Layout.SupportsNesting) {
///     ProcessChildSchemas(schema.Children);
/// }
/// if (schema.Layout.IsTabular) {
///     GenerateSqlDdl(schema);
/// }
/// </code>
/// </para>
/// </remarks>
[TypeCollection(typeof(DataLayoutBase), typeof(IDataLayout), typeof(DataLayouts))]
[ExcludeFromCodeCoverage]
public abstract partial class DataLayouts : TypeCollectionBase<DataLayoutBase, IDataLayout>
{
    // Source generator will create:
    // - Static constructor
    // - Static properties for each [TypeOption] layout
    // - All() method
    // - ById() method
    // - ByName() method
    // - Register() method (mutable)
    // - Unregister() method (mutable)
}
