using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>The types of category (FDW-1465): DataSet, which the platform defines, and Custom, which an administrator creates at runtime.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(CategoryTypeBase), typeof(ICategoryType), typeof(CategoryTypes))]
public abstract partial class CategoryTypes : TypeCollectionBase<CategoryTypeBase, ICategoryType>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
