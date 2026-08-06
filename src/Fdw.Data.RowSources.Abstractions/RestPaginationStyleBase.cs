using Fdw.Collections;

namespace Fdw.Data.RowSources.Http.Abstractions;

/// <summary>
/// Base class for REST pagination styles.
/// </summary>
public abstract class RestPaginationStyleBase : TypeOptionBase<int, RestPaginationStyleBase>, IRestPaginationStyle
{
    /// <summary>
    /// Initializes a new instance of <see cref="RestPaginationStyleBase"/>.
    /// </summary>
    protected RestPaginationStyleBase(int id, string name) : base(id, name) { }
}
