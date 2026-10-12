using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Web.RestEndpoints.Models;

/// <summary>
/// The page a paginated list endpoint cuts: how many items to skip and how many to return.
/// </summary>
/// <remarks>
/// Build it through <c>PaginationQuery</c>, which reads <c>?skip=</c> and <c>?take=</c> and resolves the
/// <c>DefaultPaginationSize</c> and <c>MaxPaginationSize</c> settings. <see cref="Take"/> is required: there is
/// no built-in page size, so a take that nothing resolved cannot reach this type.
/// </remarks>
public sealed class PaginatedListRequest
{
    /// <summary>
    /// The take of a caller no limit applies to: the largest page that still lets a reader add one to it, to see whether
    /// another page follows, without overflowing.
    /// </summary>
    /// <remarks>
    /// Why a number and not a missing take: every reader of <see cref="Take"/> already cuts a page with it, so a request
    /// with no limit returns everything through all of them at once, where a missing take would leave each one to remember
    /// the case or cut the page at nothing. Only a reader that can do better than materialising the list asks
    /// <see cref="IsUnlimited"/>.
    /// </remarks>
    public const int NoLimitTake = int.MaxValue - 1;

    /// <summary>
    /// Gets the number of items to skip from the start of the list.
    /// </summary>
    public int Skip { get; init; }

    /// <summary>
    /// Gets the number of items to return, already capped to the configured maximum.
    /// </summary>
    public required int Take { get; init; }

    /// <summary>
    /// Gets a value indicating whether no limit applies to this request: the caller named no take and the paging settings
    /// that apply to it set none, so every item from <see cref="Skip"/> on is wanted and <see cref="Take"/> is
    /// <see cref="NoLimitTake"/>.
    /// </summary>
    public bool IsUnlimited { get; init; }

    /// <summary>
    /// Cuts this page out of the full list and wraps it with the list's total.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="all">Every item the list holds, already filtered and ordered.</param>
    /// <returns>The page, with <see cref="PaginatedResponse{T}.TotalCount"/> set to the full count.</returns>
    public PaginatedResponse<T> Apply<T>(IReadOnlyList<T> all)
    {
        ArgumentNullException.ThrowIfNull(all);
        var page = all.Skip(Skip).Take(Take).ToList();
        return PaginatedResponse<T>.Create(page, Skip, Take, all.Count);
    }
}
