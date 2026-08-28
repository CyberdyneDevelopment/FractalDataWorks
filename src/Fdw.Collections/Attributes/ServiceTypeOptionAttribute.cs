using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Collections;

/// <summary>
/// Marks a class as a ServiceTypeOption that belongs to a ServiceTypeCollection.
/// Can be in any project that references the collection's assembly.
/// </summary>
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
    /// The name IS the identity. An option's Id is derived from it —
    /// <c>ServiceTypeBase.DeriveId(name)</c> — so distinct names within a collection are sufficient and the
    /// option's generic arguments are irrelevant to identity. Deriving the Id from the closed
    /// &lt;TService, TFactory&gt; pair was the previous scheme and was replaced because a domain's options
    /// routinely close the base identically: every option in <c>SessionStateTypes</c> is the same closed
    /// type, so the id was one value shared by the whole domain and
    /// <c>ServiceTypeCollectionBase.RegisterMember</c> — which keys membership on it — dropped every option
    /// after the first without a word. Two options in one collection cannot share a name without colliding
    /// in <c>ByName</c> first, which is why the name is the right source.
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
