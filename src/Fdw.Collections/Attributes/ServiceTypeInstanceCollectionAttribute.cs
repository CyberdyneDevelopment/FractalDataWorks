using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Collections;

/// <summary>
/// Marks a class as a factory-based ServiceTypeCollection that creates new instances.
/// Use when service type instances have state or are disposable.
/// </summary>
/// <remarks>
/// Generated code provides:
/// - Factory methods that create new instances (not singletons)
/// - CreateById(), CreateByName() methods returning new instances
/// - Register() method for adding factory functions at runtime
/// - All() returns registered type names (not instances)
/// - Custom lookup methods from [TypeLookup] create new instances
/// - Empty sentinel value (singleton)
/// - Child collection support via ParentCollection parameter
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage]
public sealed class ServiceTypeInstanceCollectionAttribute : Attribute
{
    /// <summary>
    /// Defines a factory-based ServiceTypeCollection that creates new instances.
    /// </summary>
    /// <param name="baseType">The abstract base type that all options inherit from. Supports unbound generics.</param>
    /// <param name="interfaceType">The interface that all options implement (used for return types). Supports unbound generics.</param>
    /// <param name="collectionType">The collection class itself (for self-reference).</param>
    /// <param name="parentCollection">Optional parent collection this collection belongs to. If null, this is a top-level collection.</param>
    /// <param name="name">Optional name for this collection when used as a child. Required if parentCollection is specified.</param>
    public ServiceTypeInstanceCollectionAttribute(
        Type baseType,
        Type interfaceType,
        Type collectionType,
        Type? parentCollection = null,
        string? name = null)
    {
        BaseType = baseType;
        InterfaceType = interfaceType;
        CollectionType = collectionType;
        ParentCollection = parentCollection;
        Name = name;
    }

    /// <summary>
    /// The abstract base type that all ServiceTypeOptions must inherit from.
    /// </summary>
    public Type BaseType { get; }

    /// <summary>
    /// The interface type used for return values. All ServiceTypeOptions must implement this.
    /// </summary>
    public Type InterfaceType { get; }

    /// <summary>
    /// The collection class itself (typically typeof(ThisClass)).
    /// </summary>
    public Type CollectionType { get; }

    /// <summary>
    /// Optional parent collection this collection belongs to.
    /// When specified, this collection becomes a child of the parent and is accessible
    /// via a static property on the parent collection.
    /// </summary>
    public Type? ParentCollection { get; }

    /// <summary>
    /// The name used for the static property accessor on the parent collection.
    /// Required when ParentCollection is specified.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// If true, only discovers ServiceTypeOptions in the current compilation (not referenced assemblies).
    /// Default is false (discovers across all referenced assemblies).
    /// </summary>
    public bool RestrictToCurrentCompilation { get; set; }

}
