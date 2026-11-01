using System;
using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Services.Abstractions;

/// <summary>
/// One page of a domain's configured members, composed as it is read.
/// </summary>
/// <typeparam name="T">The domain's implementation contract.</typeparam>
/// <remarks>
/// The page's rows are chosen before anything is composed, so <see cref="HasMore"/> is known up front
/// and the page never holds more than one composed member at a time. <see cref="Items"/> composes each
/// member as the caller enumerates it; a member that cannot be composed ends the enumeration and sets
/// <see cref="Outcome"/> to the failure. Read <see cref="Outcome"/> after <see cref="Items"/> has been
/// enumerated: before that it says only that nothing has failed yet.
/// <para>
/// There is no total count unless the caller asked for one. Counting a domain is a second query
/// over every row the filter selects, and a caller paging forward needs only to know whether there
/// is a next page. A caller that asks for a total gets it in <see cref="Total"/>; one that does not
/// gets nothing there, which means not asked, not zero.
/// </para>
/// </remarks>
public sealed class ConfigurationPage<T>
{
    private IGenericResult _outcome = GenericResult.Success();

    /// <summary>Initializes a page.</summary>
    /// <param name="skip">How many members precede this page.</param>
    /// <param name="take">How many members this page holds at most.</param>
    /// <param name="hasMore">Whether members follow this page.</param>
    /// <param name="items">
    /// Builds the page's members. It is handed the setter for <see cref="Outcome"/>, which it calls with
    /// the failure that ends the enumeration.
    /// </param>
    /// <param name="total">
    /// How many members the query selects in all, or <see langword="null"/> when the caller did not ask.
    /// </param>
    public ConfigurationPage(int skip, int take, bool hasMore, Func<Action<IGenericResult>, IAsyncEnumerable<T>> items, int? total = null)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        Skip = skip;
        Take = take;
        HasMore = hasMore;
        Total = total;
        Items = items(failure => _outcome = failure);
    }

    /// <summary>Gets how many members precede this page.</summary>
    public int Skip { get; }

    /// <summary>Gets how many members this page holds at most.</summary>
    public int Take { get; }

    /// <summary>Gets whether members follow this page.</summary>
    public bool HasMore { get; }

    /// <summary>Gets how many members the query selects in all, or nothing when no total was asked for.</summary>
    public int? Total { get; }

    /// <summary>Gets the page's members, composed as they are enumerated.</summary>
    public IAsyncEnumerable<T> Items { get; }

    /// <summary>Gets how the enumeration of <see cref="Items"/> ended.</summary>
    public IGenericResult Outcome => _outcome;
}
