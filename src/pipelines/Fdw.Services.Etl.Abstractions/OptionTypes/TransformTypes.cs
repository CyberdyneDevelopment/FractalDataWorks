using Fdw.Configuration;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Collection of transform types for ETL pipelines.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(TransformTypeBase), typeof(ITransformType), typeof(TransformTypes))]
public abstract partial class TransformTypes : TypeCollectionBase<TransformTypeBase, ITransformType>
{
    // DO NOT IMPLEMENT BY HAND!
    // Source generator automatically creates static TransformTypes class with:
    // - TransformTypes.Map (returns TransformTypeBase)
    // - TransformTypes.Filter (returns TransformTypeBase)
    // - TransformTypes.Aggregate (returns TransformTypeBase)
    // - TransformTypes.Calculate (returns TransformTypeBase)
    // - TransformTypes.Lookup (returns TransformTypeBase)
    // - TransformTypes.All (collection of TransformTypeBase)
    // - TransformTypes.ById(int id) (returns TransformTypeBase)
    // - TransformTypes.ByName(string name) (returns TransformTypeBase)
}
