using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Closed TypeCollection of result-code handling categories (1..11). The category is the leading
/// band of a code's number (number / 10000); failure/retryable/HTTP behavior lives on each option.
/// Bands 1..9 are 5-digit codes; bands 10 (Forbidden/403) and 11 (GatewayTimeout/504) use 6-digit
/// codes (100000+), reached via the same number / 10000 mechanism.
/// FDW owns all options — consumers never add categories, only specific codes in the open band.
/// </summary>
[TypeCollection(typeof(ResultCategoryBase), typeof(IResultCategory), typeof(ResultCategories))]
[ExcludeFromCodeCoverage]
public abstract partial class ResultCategories : TypeCollectionBase<ResultCategoryBase, IResultCategory>
{
}
