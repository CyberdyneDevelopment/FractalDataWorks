using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Collections;

/// <summary>
/// Marks a class as a ServiceTypeOption that belongs to a ServiceTypeCollection.
/// Can be in any project that references the collection's assembly.
/// </summary>
// Why: pure attribute definition (declarative metadata only, consumed by source generators) — no logic to unit test.
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage]
public sealed class ServiceTypeOptionAttribute : Attribute
{
    /// <summary>
    /// Marks this class as a ServiceTypeOption belonging to the specified collection.
    /// </summary>
    /// <param name="collectionType">The ServiceTypeCollection this option belongs to.</param>
    /// <param name="name">
    /// The name used for the static property accessor and ByName() lookup.
    /// Must be unique within the collection and a valid C# identifier.
    /// </param>
    /// <remarks>
    /// The name does NOT feed Id generation. A ServiceTypeOption's Id is
    /// <c>MD5($"{TService.FullName}:{TFactory.FullName}")</c> (see <c>ServiceTypeBase.Id</c>), so two options
    /// with different names but the same closed &lt;TService, TFactory&gt; pair share an Id — and the generated
    /// <c>RegisterMember</c> silently discards the second. Uniqueness comes from each option closing the base's
    /// open type parameters over distinct types, not from the name. <c>ST001</c> catches collisions only within
    /// the collection's own compilation; cross-assembly options are invisible to it.
    /// </remarks>
    public ServiceTypeOptionAttribute(Type collectionType, string name)
    {
        CollectionType = collectionType;
        Name = name;
    }

    /// <summary>
    /// The ServiceTypeCollection this option belongs to.
    /// Supports unbound generics: typeof(MyCollection&lt;,&gt;)
    /// </summary>
    public Type CollectionType { get; }

    /// <summary>
    /// The name used for the static property, ByName() lookup, and Id generation.
    /// </summary>
    public string Name { get; }
}
