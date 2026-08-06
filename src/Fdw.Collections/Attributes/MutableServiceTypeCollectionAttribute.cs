using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Collections;

/// <summary>
/// Marks a class as a mutable ServiceTypeCollection using ConcurrentDictionary.
/// Use for plugin systems that require runtime service type registration.
/// </summary>
/// <remarks>
/// Generated code provides:
/// - ConcurrentDictionary-based lookups (thread-safe)
/// - Register(TInterface) method for runtime additions
/// - Static property accessors for compile-time ServiceTypeOptions
/// - ById(), ByName(), All() methods
/// - Custom lookup methods from [TypeLookup] attributes
/// - Empty sentinel value
/// - Register(IServiceCollection, ILoggerFactory?) invoker over the swappable RegistrationMethod field
/// - Child collection support via ParentCollection parameter
/// </remarks>
// Why: pure attribute definition (declarative metadata only, consumed by source generators) — no logic to unit test.
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage]
public sealed class MutableServiceTypeCollectionAttribute : Attribute
{
    /// <summary>
    /// Defines a mutable ServiceTypeCollection with runtime registration support.
    /// </summary>
    /// <param name="baseType">The abstract base type that all options inherit from. Supports unbound generics.</param>
    /// <param name="interfaceType">The interface that all options implement (used for return types). Supports unbound generics.</param>
    /// <param name="collectionType">The collection class itself (for self-reference).</param>
    /// <param name="parentCollection">Optional parent collection this collection belongs to. If null, this is a top-level collection.</param>
    /// <param name="name">Optional name for this collection when used as a child. Required if parentCollection is specified.</param>
    public MutableServiceTypeCollectionAttribute(
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
