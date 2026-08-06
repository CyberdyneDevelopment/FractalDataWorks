using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Collection of join types for data operations.
/// </summary>
/// <remarks>
/// <para>
/// Source generator discovers all types marked with [TypeOption(typeof(JoinTypes), ...)].
/// Source generator also creates static properties for each type: None, Inner, Left, Right, Full, Cross.
/// </para>
/// <para>
/// Usage:
/// <code>
/// // Access via static properties
/// var inner = JoinTypes.Inner;
/// var left = JoinTypes.Left;
///
/// // Or lookup by name/id
/// var joinType = JoinTypes.ByName("Inner");
/// var joinType = JoinTypes.ById(1);
///
/// // Get all join types
/// var all = JoinTypes.All();
/// </code>
/// </para>
/// </remarks>
[TypeCollection(typeof(JoinTypeBase), typeof(IJoinType), typeof(JoinTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class JoinTypes : TypeCollectionBase<JoinTypeBase, IJoinType>
{
    // Source generator will implement:
    // - public static IJoinType None { get; }
    // - public static IJoinType Inner { get; }
    // - public static IJoinType Left { get; }
    // - public static IJoinType Right { get; }
    // - public static IJoinType Full { get; }
    // - public static IJoinType Cross { get; }
    // - public static FrozenDictionary<int, IJoinType> All()
    // - public static IJoinType ById(int id)
    // - public static IJoinType ByName(string name)
    // - public static void Register(IJoinType type)
    // - public static IJoinType Empty
}
