using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources;

/// <summary>
/// TypeCollection for row mapper types (Pooled, Dynamic).
/// </summary>
/// <remarks>
/// Row mappers are lightweight utilities that don't require per-instance DI,
/// so they're TypeCollection members rather than ServiceTypes.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(RowMapperTypeBase), typeof(IRowMapperType), typeof(RowMapperTypes))]
public abstract partial class RowMapperTypes : TypeCollectionBase<RowMapperTypeBase, IRowMapperType>
{
}
