using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Http.Abstractions;

/// <summary>
/// TypeCollection for REST pagination styles.
/// </summary>
[TypeCollection(typeof(RestPaginationStyleBase), typeof(IRestPaginationStyle), typeof(RestPaginationStyles))]
[ExcludeFromCodeCoverage]
public abstract partial class RestPaginationStyles : TypeCollectionBase<RestPaginationStyleBase, IRestPaginationStyle> { }
