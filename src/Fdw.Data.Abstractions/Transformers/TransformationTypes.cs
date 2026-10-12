using Fdw.Collections;
using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Data.Abstractions;

/// <summary>
/// TypeCollection for all data transformer type implementations.
/// Transformers apply ETL-style transformations to data (aggregation, filtering, calculations, etc.).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(TransformationTypeBase), typeof(ITransformationType), typeof(TransformationTypes), RestrictToCurrentCompilation = false)]
public sealed partial class TransformationTypes : TypeCollectionBase<TransformationTypeBase, ITransformationType>
{
    // TypeCollectionGenerator will generate all members
}
