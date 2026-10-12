using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// A DataSet category loaded from <c>data.DataSetCategory</c> at runtime.
/// Registered into <see cref="DataSetCategories"/> by <see cref="DataSetCategoryProvider"/>
/// during the Initialize phase.
/// </summary>
/// <remarks>
/// Compile-time categories are declared with <c>[TypeOption(typeof(DataSetCategories), "Name")]</c>
/// and do not require a database row. This class handles the DB-backed side of the
/// Model C hybrid (compile-time defaults + deployment-specific runtime extensions).
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class RuntimeDataSetCategory : DataSetCategoryBase
{
    /// <summary>
    /// Initializes a new instance from a configuration record loaded from <c>data.DataSetCategory</c>.
    /// </summary>
    /// <param name="configuration">The configuration record to wrap. Must have a non-blank Name.</param>
    public RuntimeDataSetCategory(DataSetCategoryConfiguration configuration)
        : base(configuration.SortOrder, configuration.Name)
    {
    }
}
