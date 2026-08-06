using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Collections.Attributes;

/// <summary>
/// Marks a class as a type collection for source generation.
/// Applied to classes to enable efficient discovery and generation of high-performance collections.
/// </summary>
/// <param name="baseType">The base type to collect (e.g., typeof(MyBaseType)).</param>
/// <param name="defaultReturnType">The default return type for generated methods (e.g., typeof(IMyInterface)).</param>
/// <param name="collectionType">The partial class type being generated (e.g., typeof(MyTypes)).</param>
// Why: pure attribute definition (declarative metadata only, consumed by source generators) — no logic to unit test.
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage]
public sealed class TypeCollectionAttribute(Type baseType, Type defaultReturnType, Type collectionType) : Attribute
{
    /// <summary>
    /// Gets the base type to collect.
    /// </summary>
    public Type BaseType { get; } = baseType ?? throw new ArgumentNullException(nameof(baseType));

    /// <summary>
    /// Gets the default return type for generated methods.
    /// </summary>
    public Type DefaultReturnType { get; } = defaultReturnType ?? throw new ArgumentNullException(nameof(defaultReturnType));

    /// <summary>
    /// Gets the partial class type being generated.
    /// </summary>
    public Type CollectionType { get; } = collectionType ?? throw new ArgumentNullException(nameof(collectionType));

    /// <summary>
    /// Gets the fully qualified name of the base type (for generator compatibility).
    /// </summary>
    public string BaseTypeName => BaseType.FullName ?? BaseType.Name;

    /// <summary>
    /// Gets the collection name (for generator compatibility).
    /// </summary>
    public string CollectionName => CollectionType.Name;

    /// <summary>
    /// Gets or sets a value indicating whether to generate methods for access instead of properties.
    /// When true, generates methods like <c>Opening()</c> instead of properties like <c>Opening</c>.
    /// Default is false (generates properties).
    /// </summary>
    public bool UseMethods { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to restrict TypeOption discovery to the current compilation only.
    /// When true, the generator only scans the current compilation for [TypeOption] attributes (single-assembly pattern).
    /// When false, the generator scans all referenced assemblies for [TypeOption] attributes (cross-assembly pattern).
    /// Default is false (cross-assembly support enabled).
    /// </summary>
    /// <remarks>
    /// <para><b>Use true when:</b> All TypeOption implementations are in the same assembly as the TypeCollection</para>
    /// <para><b>Use false when:</b> TypeOption implementations are distributed across multiple assemblies or NuGet packages</para>
    /// </remarks>
    public bool RestrictToCurrentCompilation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to generate a Blazor UI component for this TypeCollection.
    /// When true, generates a {CollectionName}Selector.razor component with a dropdown/selector for all options.
    /// Default is false (no UI component generated).
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, generates a reusable Blazor component that can be used in configuration UIs:
    /// <code>
    /// &lt;FilterOperatorsSelector @bind-Value="model.Operator" /&gt;
    /// </code>
    /// </para>
    /// <para>
    /// The generated component includes:
    /// <list type="bullet">
    /// <item>Dropdown with all TypeOptions from the collection</item>
    /// <item>Two-way binding support via @bind-Value</item>
    /// <item>Display names from TypeOption attributes</item>
    /// <item>ID-based value tracking for serialization</item>
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
    /// Use this to provide a custom Blazor component for rendering the TypeCollection selector.
    /// The custom component must implement the standard @bind-Value pattern with a parameter
    /// of type matching the DefaultReturnType.
    /// </para>
    /// </remarks>
    public Type? UIComponent { get; set; }

    /// <summary>
    /// Gets or sets the parent collection type if this collection is a member of a parent collection.
    /// Must be used together with <see cref="TypeOptionName"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Example:
    /// <code>
    /// // Parent collection (auto-detected)
    /// [TypeCollection(typeof(DataTypeConverterCollectionBase),
    ///                 typeof(IDataTypeConverters),
    ///                 typeof(DataTypeConverters))]
    /// public partial class DataTypeConverters { }
    ///
    /// // Child collection
    /// [TypeCollection(typeof(DataTypeConverterBase),
    ///                 typeof(IDataTypeConverter),
    ///                 typeof(MsSqlDataTypeConverters),
    ///                 TypeOption = typeof(DataTypeConverters),
    ///                 TypeOptionName = "MsSql")]
    /// public partial class MsSqlDataTypeConverters { }
    /// </code>
    /// </para>
    /// </remarks>
    public Type? TypeOption { get; set; }

    /// <summary>
    /// Gets or sets the name to use when this collection is registered in the parent collection.
    /// Required when <see cref="TypeOption"/> is set.
    /// This becomes the accessor name in the parent (e.g., "MsSql" becomes DataTypeConverters.MsSql).
    /// </summary>
    public string? TypeOptionName { get; set; }

    /// <summary>
    /// Gets a value indicating whether this collection is a child of another collection.
    /// True when both <see cref="TypeOption"/> and <see cref="TypeOptionName"/> are set.
    /// </summary>
    public bool IsChildCollection => TypeOption != null && !string.IsNullOrEmpty(TypeOptionName);
}