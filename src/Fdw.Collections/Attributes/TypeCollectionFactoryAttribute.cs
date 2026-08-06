using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Collections.Attributes;

/// <summary>
/// Marks a class as a factory collection that generates factory methods for creating command/option instances.
/// The source generator will discover [TypeOption] types and generate corresponding factory methods.
/// </summary>
/// <remarks>
/// <para>
/// This differs from [TypeCollection] which discovers singletons.
/// TypeCollectionFactory generates factory METHODS for creating new instances.
/// </para>
/// <para>
/// Example:
/// <code>
/// [TypeCollectionFactory(typeof(DataCommandBase), typeof(IDataCommand), allowNesting: true)]
/// public abstract partial class DataCommands
/// {
///     // Generated: public static QueryCommand&lt;T&gt; Query&lt;T&gt;(string containerName)
///     // Generated: public static class Mutation { ... }
/// }
/// </code>
/// </para>
/// </remarks>
// Why: pure attribute definition (declarative metadata only, consumed by source generators) — no logic to unit test.
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public class TypeCollectionFactoryAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeCollectionFactoryAttribute"/> class.
    /// </summary>
    /// <param name="baseType">The base type to discover (e.g., DataCommandBase). Must have [TypeOption] implementations.</param>
    /// <param name="interfaceType">Optional interface type that discovered types implement. Improves type safety in generated code.</param>
    /// <param name="allowNesting">If true, enables discovery of [NestedTypeCollectionFactory] collections as nested static classes.</param>
    public TypeCollectionFactoryAttribute(Type baseType, Type? interfaceType = null, bool allowNesting = false)
    {
        BaseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
        InterfaceType = interfaceType;
        AllowNesting = allowNesting;
    }

    /// <summary>
    /// Gets the base type to discover implementations of.
    /// </summary>
    public Type BaseType { get; }

    /// <summary>
    /// Gets the optional interface type for type safety.
    /// </summary>
    public Type? InterfaceType { get; }

    /// <summary>
    /// Gets a value indicating whether nested collections are allowed.
    /// If true, [NestedTypeCollectionFactory] collections will be generated as nested static classes.
    /// </summary>
    public bool AllowNesting { get; }

    /// <summary>
    /// Gets or sets a value indicating whether to generate a Blazor UI component for this TypeCollectionFactory.
    /// When true, generates a {CollectionName}Selector.razor component with a dropdown/selector for all factory options.
    /// Default is false (no UI component generated).
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, generates a reusable Blazor component that can be used in configuration UIs:
    /// <code>
    /// &lt;DataCommandsSelector @bind-CommandType="model.CommandType" /&gt;
    /// </code>
    /// </para>
    /// <para>
    /// The generated component includes:
    /// <list type="bullet">
    /// <item>Dropdown with all factory options from the collection</item>
    /// <item>Two-way binding support via @bind-CommandType or @bind-Value</item>
    /// <item>Display names from TypeOption attributes</item>
    /// <item>Support for nested factory collections</item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool GenerateUIComponent { get; set; }

    /// <summary>
    /// Gets or sets the custom UI component type to use instead of the auto-generated component.
    /// When specified, the generator will not create a default component and will instead
    /// reference this custom component type in helper methods.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this to provide a custom Blazor component for rendering the factory selector.
    /// The custom component must implement appropriate binding patterns for factory method selection.
    /// </para>
    /// </remarks>
    public Type? UIComponent { get; set; }
}
