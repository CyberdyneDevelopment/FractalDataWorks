using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Collections.Attributes;

/// <summary>
/// Marks a class as a factory collection that creates new instances on each access.
/// Applied to classes to enable efficient discovery and generation of factory-based collections.
/// Generated collections use factory methods that create new instances per call.
/// Mutable - supports Register() method for runtime factory registration.
/// </summary>
/// <param name="baseType">The base type to collect (e.g., typeof(MyBaseType)).</param>
/// <param name="defaultReturnType">The default return type for generated methods (e.g., typeof(IMyInterface)).</param>
/// <param name="collectionType">The partial class type being generated (e.g., typeof(MyFactories)).</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage]
public sealed class TypeInstanceCollectionAttribute(Type baseType, Type defaultReturnType, Type collectionType) : Attribute
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
    /// When true, generates methods like <c>CreateOpening()</c> instead of <c>CreateOpening</c>.
    /// Default is false (generates methods - factories are typically methods).
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
    public bool GenerateUIComponent { get; set; }

    /// <summary>
    /// Gets or sets the custom UI component type to use instead of the auto-generated component.
    /// When specified, the generator will not create a default component and will instead
    /// reference this custom component type in helper methods.
    /// </summary>
    public Type? UIComponent { get; set; }

    /// <summary>
    /// Gets or sets the TypeOption attribute if this collection is a member of a parent collection.
    /// </summary>
    public Type? TypeOption { get; set; }

    /// <summary>
    /// Gets or sets the name to use when this collection is registered in the parent collection.
    /// Required when <see cref="TypeOption"/> is set.
    /// </summary>
    public string? TypeOptionName { get; set; }
}
