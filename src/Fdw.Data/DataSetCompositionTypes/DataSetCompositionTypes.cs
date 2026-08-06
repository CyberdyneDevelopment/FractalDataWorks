using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Collection of composition strategies that describe how a <see cref="IDataSet"/> combines its sources.
/// </summary>
/// <remarks>
/// <para>
/// Source generator discovers all types marked with [TypeOption(typeof(DataSetCompositionTypes), ...)].
/// Source generator also creates static properties for each type: Singular, Join, Union.
/// </para>
/// <para>
/// Usage:
/// <code>
/// // Access via static properties
/// var singular = DataSetCompositionTypes.Singular;
/// var join = DataSetCompositionTypes.Join;
///
/// // Or lookup by name/id
/// var type = DataSetCompositionTypes.ByName("Union");
/// var type = DataSetCompositionTypes.ById(1);
///
/// // Get all composition types
/// var all = DataSetCompositionTypes.All();
/// </code>
/// </para>
/// </remarks>
[TypeCollection(typeof(DataSetCompositionTypeBase), typeof(IDataSetCompositionType), typeof(DataSetCompositionTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class DataSetCompositionTypes : TypeCollectionBase<DataSetCompositionTypeBase, IDataSetCompositionType>
{
    // Source generator will implement:
    // - public static IDataSetCompositionType Singular { get; }
    // - public static IDataSetCompositionType Join { get; }
    // - public static IDataSetCompositionType Union { get; }
    // - public static FrozenDictionary<int, IDataSetCompositionType> All()
    // - public static IDataSetCompositionType ById(int id)
    // - public static IDataSetCompositionType ByName(string name)
    // - public static void Register(IDataSetCompositionType type)
    // - public static IDataSetCompositionType Empty
}
