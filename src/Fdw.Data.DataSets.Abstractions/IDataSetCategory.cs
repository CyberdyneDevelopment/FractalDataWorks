using Fdw.Collections;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Marker interface for DataSet category type options.
/// Implemented by <see cref="DataSetCategoryBase"/> and any compile-time
/// <c>[TypeOption]</c>-decorated subclasses that packages ship as defaults.
/// </summary>
/// <remarks>
/// DataSet categories follow Model C (Hybrid): compile-time TypeOptions register
/// at assembly load via source-generated module initializers; deployment-specific
/// categories are loaded from <c>data.DataSetCategory</c> via the provider
/// Configure/Register/Initialize three-phase and added to
/// <see cref="DataSetCategories"/> at startup.
/// </remarks>
public interface IDataSetCategory : ITypeOption<int>
{
}
