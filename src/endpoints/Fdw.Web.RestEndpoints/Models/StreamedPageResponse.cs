using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Fdw.Web.RestEndpoints.Models;

/// <summary>
/// One page of a list, written to the response as its items are produced.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <remarks>
/// The properties are written in the order declared. <see cref="Items"/> is written as each item is
/// produced; <see cref="Error"/> is written after it and read only then, so it reports how producing
/// the items ended. By then the status code has been sent: a page that fails part way arrives as
/// a 200 carrying the items produced before the failure and the failure itself. A client reads
/// <see cref="Error"/> before trusting <see cref="Items"/>.
/// <para>
/// There is no total count unless the request asked for one. <see cref="HasMore"/> says whether the next
/// page, at <see cref="Skip"/> plus <see cref="Take"/>, has anything in it. A page that carries
/// <see cref="Total"/> was asked for it, and the property is left out of the response otherwise: an
/// absent total means not asked, where a zero would say the list is empty.
/// </para>
/// <para>
/// A page whose request asked for something to be left out (<c>?description=false</c> on the data set list) says what it left out in <see cref="LeftOut"/>, so a client
/// can tell a member left out from a member that has none. The property is left out of the response when nothing was asked to be left out (FDW-6124).
/// </para>
/// </remarks>
public sealed class StreamedPageResponse<T>
{
    private readonly Func<ProblemDetails?> _error;

    /// <summary>Initializes a page.</summary>
    /// <param name="skip">How many items precede this page.</param>
    /// <param name="take">How many items this page holds at most.</param>
    /// <param name="hasMore">Whether items follow this page.</param>
    /// <param name="items">The page's items, produced as they are written.</param>
    /// <param name="error">Reads how producing the items ended; asked only after they are written.</param>
    /// <param name="total">How many items the query selects in all, or <see langword="null"/> when the caller did not ask.</param>
    /// <param name="leftOut">The names of the members of each item the request asked to leave out, or <see langword="null"/> when it asked for none.</param>
    public StreamedPageResponse(
        int skip, int take, bool hasMore, IAsyncEnumerable<T> items, Func<ProblemDetails?> error, int? total = null, IReadOnlyList<string>? leftOut = null)
    {
        Skip = skip;
        Take = take;
        HasMore = hasMore;
        Total = total;
        LeftOut = leftOut;
        Items = items;
        _error = error;
    }

    /// <summary>Gets how many items precede this page.</summary>
    [JsonPropertyOrder(0)]
    public int Skip { get; }

    /// <summary>Gets how many items this page holds at most.</summary>
    [JsonPropertyOrder(1)]
    public int Take { get; }

    /// <summary>Gets whether items follow this page.</summary>
    [JsonPropertyOrder(2)]
    public bool HasMore { get; }

    /// <summary>Gets how many items the query selects in all, or nothing when the caller did not ask.</summary>
    [JsonPropertyOrder(3)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Total { get; }

    /// <summary>Gets the names of the members of each item the request asked to leave out, or nothing when it asked for none.</summary>
    [JsonPropertyOrder(4)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? LeftOut { get; }

    /// <summary>Gets the page's items.</summary>
    [JsonPropertyOrder(5)]
    public IAsyncEnumerable<T> Items { get; }

    /// <summary>Gets why the page ended before its last item, or nothing when every item was produced.</summary>
    [JsonPropertyOrder(6)]
    public ProblemDetails? Error => _error();
}
