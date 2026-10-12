using Fdw.Collections;
using Fdw.Data;

namespace Fdw.Web.RestEndpoints.Pagination;

/// <summary>
/// How a list's search text is compared with a name: the word a request names it by, and the filter operator that
/// states what it means. The search never switches on the word.
/// </summary>
public interface IListMatch : ITypeOption<int, ListMatchBase>
{
    /// <summary>Gets the word a request names the match by (<c>?match=</c>), in the case the API documents.</summary>
    string Wire { get; }

    /// <summary>Gets the filter operator that compares a name with the search text.</summary>
    IFilterOperator Operator { get; }
}
