using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Registry of dataset types. Pure type collection with no DI orchestration.
/// DI registration is handled by DataSetProvider.
/// </summary>
/// <remarks>
/// Uses [MutableTypeCollection] to support cross-assembly TypeOption registration
/// (e.g., WorkflowDefinitionsDataSet in Orchestration.Workflows).
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DataSetTypeBase), typeof(IDataSetType), typeof(DataSetTypes))]
public abstract partial class DataSetTypes : TypeCollectionBase<DataSetTypeBase, IDataSetType>
{
    /// <summary>
    /// The service category for database configuration loading.
    /// </summary>
    public static string ServiceCategory => "DataSet";
}
