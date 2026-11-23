using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>The types a calculation can yield for a filter, each found by its CLR type.</summary>
/// <remarks>A type no kind holds answers the NotFound sentinel; the caller refuses the calculation.</remarks>
[TypeCollection(typeof(FilterCalculationResultKindBase), typeof(IFilterCalculationResultKind), typeof(FilterCalculationResultKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class FilterCalculationResultKinds : TypeCollectionBase<FilterCalculationResultKindBase, IFilterCalculationResultKind>
{
}
