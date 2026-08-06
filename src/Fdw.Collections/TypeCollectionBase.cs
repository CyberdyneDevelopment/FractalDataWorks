using System;
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
/// TypeCollections are themselves TypeOptions with int Ids (hash of collection name).
/// This allows collections to be nested (parent-child via MemberOf).
/// </para>
/// <para>
/// The TKey for TypeOption lookups is extracted from TBase's ITypeOption implementation.
/// For example: CommandTypeBase : ITypeOption&lt;int, CommandTypeBase&gt; → TKey = int
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class TypeCollectionBase<TBase> : TypeOptionBase<int, TypeCollectionBase<TBase>>
    where TBase : class
{
    /// <summary>
    /// Initializes a new instance of TypeCollectionBase with auto-generated Id and Name.
    /// </summary>
    protected TypeCollectionBase() : base(GenerateIdFromTypeName(), GenerateNameFromTypeName())
    {
    }

    private static int GenerateIdFromTypeName()
    {
        // Hash of type name for unique Id
        return StringComparer.Ordinal.GetHashCode(typeof(TypeCollectionBase<TBase>).Name);
    }

    private static string GenerateNameFromTypeName()
    {
        // Extract name from generic type
        var typeName = typeof(TypeCollectionBase<TBase>).Name;
        return typeName;
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
/// TypeCollections are themselves TypeOptions with int Ids (hash of collection name).
/// This allows collections to be nested (parent-child via MemberOf).
/// </para>
/// <para>
/// The TKey for TypeOption lookups is extracted from TBase's ITypeOption implementation.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class TypeCollectionBase<TBase, TGeneric> : TypeOptionBase<int, TypeCollectionBase<TBase, TGeneric>>
    where TBase : class, TGeneric
    where TGeneric : class
{
    /// <summary>
    /// Initializes a new instance of TypeCollectionBase with auto-generated Id and Name.
    /// </summary>
    protected TypeCollectionBase() : base(GenerateIdFromTypeName(), GenerateNameFromTypeName())
    {
    }

    private static int GenerateIdFromTypeName()
    {
        // Hash of type name for unique Id
        return StringComparer.Ordinal.GetHashCode(typeof(TypeCollectionBase<TBase, TGeneric>).Name);
    }

    private static string GenerateNameFromTypeName()
    {
        // Extract name from generic type
        var typeName = typeof(TypeCollectionBase<TBase, TGeneric>).Name;
        return typeName;
    }
}