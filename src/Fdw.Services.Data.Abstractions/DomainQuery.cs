using Fdw.Data.Abstractions;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// What a caller asks of a domain's list: which members, in what order, which page, and whether it
/// wants to know how many members the filter selects in all.
/// </summary>
/// <remarks>
/// A query is expressed in the same filter and ordering expressions every other data command uses, so
/// it means the same thing on every connection kind. The filter and ordering name fields of the
/// domain's record, never of its implementation: a domain list is answered from the domain table
/// alone, and a field the domain record does not carry cannot be filtered or sorted on.
/// <para>
/// The provider completes the ordering with the container's declared keys, so a page is a page of
/// something in a fixed order whatever the caller asked for.
/// </para>
/// </remarks>
public sealed class DomainQuery
{
    /// <summary>Initializes a query.</summary>
    /// <param name="filter">Which members to select, or <see langword="null"/> for every current member.</param>
    /// <param name="ordering">The order the caller wants, most significant first, or <see langword="null"/> for the key order.</param>
    /// <param name="skip">How many selected members precede the page.</param>
    /// <param name="take">How many members the page holds at most.</param>
    /// <param name="includeTotal">Whether the page carries how many members the filter selects in all.</param>
    public DomainQuery(
        IFilterExpression? filter,
        IOrderingExpression? ordering,
        int skip,
        int take,
        bool includeTotal)
    {
        Filter = filter;
        Ordering = ordering;
        Skip = skip;
        Take = take;
        IncludeTotal = includeTotal;
    }

    /// <summary>Gets which members to select, or nothing for every current member.</summary>
    public IFilterExpression? Filter { get; }

    /// <summary>Gets the order the caller wants, or nothing for the key order.</summary>
    public IOrderingExpression? Ordering { get; }

    /// <summary>Gets how many selected members precede the page.</summary>
    public int Skip { get; }

    /// <summary>Gets how many members the page holds at most.</summary>
    public int Take { get; }

    /// <summary>Gets whether the page carries how many members the filter selects in all.</summary>
    public bool IncludeTotal { get; }
}
