using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Abstract base class for DataSet category type options.
/// Derive from this class to declare compile-time DataSet categories via
/// the <c>[TypeOption(typeof(DataSetCategories), "CategoryName")]</c> attribute.
/// </summary>
/// <remarks>
/// Runtime (DB-backed) categories are added to <see cref="DataSetCategories"/>
/// by the provider at startup; they also derive from this class but are
/// constructed dynamically rather than through source-generation.
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class DataSetCategoryBase
    : TypeOptionBase<int, DataSetCategoryBase>, IDataSetCategory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetCategoryBase"/> class.
    /// </summary>
    /// <param name="id">
    /// Unique numeric identifier. Use a positive integer consistent across deployments
    /// for compile-time (source-gen) categories. DB-backed categories use their DB row ordinal.
    /// </param>
    /// <param name="name">The display name and lookup key for this category.</param>
    protected DataSetCategoryBase(int id, string name)
        : base(id, name)
    {
    }
}
