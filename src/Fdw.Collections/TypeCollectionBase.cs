using System.Diagnostics.CodeAnalysis;

namespace Fdw.Collections;

/// <summary>
/// Base class for type collections that provides inheritance structure for the TypeCollectionGenerator.
/// The TypeCollectionGenerator will populate all methods and properties in the generated partial class.
/// Inherits from TypeOptionBase to provide Id/Name/Category for the collection itself.
/// </summary>
/// <typeparam name="TBase">The type option that collection items must derive from</typeparam>
/// <remarks>
/// <para>
/// TypeCollections are themselves TypeOptions with int Ids (FNV-1a of the collection's fully-qualified type name).
/// This allows collections to be nested (parent-child via MemberOf).
/// </para>
/// <para>
/// The TKey for TypeOption lookups is extracted from TBase's ITypeOption implementation.
/// For example: CommandTypeBase : ITypeOption&lt;int, CommandTypeBase&gt; → TKey = int
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class TypeCollectionBase<TBase> : TypeOptionBase<TypeCollectionBase<TBase>>
    where TBase : class
{
    /// <summary>
    /// Initializes a new instance of the collection.
    /// </summary>
    /// <remarks>
    /// The Id is the FNV-1a hash of this collection's own fully-qualified type name, derived by
    /// <see cref="TypeOptionBase{TBase}"/> the way every TypeOption's is. The name is that of the member
    /// base it collects. Both used to come from the open generic's name, which is the same for every
    /// collection, hashed with the per-process randomized string hash - so every collection that is
    /// itself an option reported the same Id and Name (FDW-919).
    /// </remarks>
    protected TypeCollectionBase() : base(typeof(TBase).Name)
    {
    }
}

/// <summary>
/// Base class for type collections with different return type.
/// Use this when you want the collection to work with TBase types but return TGeneric instances.
/// Inherits from TypeOptionBase to provide Id/Name/Category for the collection itself.
/// </summary>
/// <typeparam name="TBase">The concrete base type that collection items derive from</typeparam>
/// <typeparam name="TGeneric">The return type for all collection methods (must be base of TBase)</typeparam>
/// <remarks>
/// <para>
/// TypeCollections are themselves TypeOptions with int Ids (FNV-1a of the collection's fully-qualified type name).
/// This allows collections to be nested (parent-child via MemberOf).
/// </para>
/// <para>
/// The TKey for TypeOption lookups is extracted from TBase's ITypeOption implementation.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class TypeCollectionBase<TBase, TGeneric> : TypeOptionBase<TypeCollectionBase<TBase, TGeneric>>
    where TBase : class, TGeneric
    where TGeneric : class
{
    /// <summary>
    /// Initializes a new instance of the collection.
    /// </summary>
    /// <remarks>
    /// The Id is the FNV-1a hash of this collection's own fully-qualified type name, derived by
    /// <see cref="TypeOptionBase{TBase}"/> the way every TypeOption's is. The name is that of the member
    /// base it collects. Both used to come from the open generic's name, which is the same for every
    /// collection, hashed with the per-process randomized string hash - so every collection that is
    /// itself an option reported the same Id and Name (FDW-919).
    /// </remarks>
    protected TypeCollectionBase() : base(typeof(TBase).Name)
    {
    }
}