using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Registry of DataSet category type options (Model C — Hybrid).
/// </summary>
/// <remarks>
/// <para>
/// Compile-time categories are declared with
/// <c>[TypeOption(typeof(DataSetCategories), "CategoryName")]</c> and registered
/// automatically at assembly load via source-generated module initializers.
/// </para>
/// <para>
/// Runtime (DB-backed) categories are loaded from <c>data.DataSetCategory</c>
/// and added via <c>Add()</c> during the provider Initialize phase.
/// Both kinds are enumerated via <c>DataSetCategories.All()</c>.
/// </para>
/// <para>
/// FDW ships no compile-time default categories. Packages built on FDW declare
/// their own via <c>[TypeOption]</c>; deployments extend the list through admin UI.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[MutableTypeCollection(typeof(DataSetCategoryBase), typeof(IDataSetCategory), typeof(DataSetCategories))]
public abstract partial class DataSetCategories
    : TypeCollectionBase<DataSetCategoryBase, IDataSetCategory>
{
}
