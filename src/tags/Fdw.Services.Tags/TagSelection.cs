using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Tags.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Tags;

/// <summary>
/// The things of one kind that carry every tag a list request named, or nothing when the request named none (FDW-3816).
/// </summary>
/// <remarks>
/// A list that is asked <c>?tag=a&amp;tag=b</c> keeps the members that carry both. The selection is the Ids of those members; a list narrows
/// its own read with <see cref="Page{T}"/>. A request that names tags to a host that does not run the Tags service is refused, never answered
/// as an unfiltered list.
/// </remarks>
public sealed class TagSelection
{
    /// <summary>The take that reads every row: the largest a domain provider accepts.</summary>
    public const int EveryRow = int.MaxValue - 1;

    private readonly HashSet<Guid>? _ids;

    private TagSelection(HashSet<Guid>? ids)
    {
        _ids = ids;
    }

    /// <summary>Gets the selection of a request that named no tag: every member is kept.</summary>
    public static TagSelection None { get; } = new(null);

    /// <summary>Gets whether the request named tags, so the list is narrowed.</summary>
    public bool Applies => _ids is not null;

    /// <summary>Finds the things of a kind that carry every one of the named tags.</summary>
    /// <param name="tagNames">The tags the request named; none when it named none.</param>
    /// <param name="tags">The Tags service, or <see langword="null"/> on a host that does not run it.</param>
    /// <param name="kind">The kind of the listed things, a name in <c>TaggableKinds</c>.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The selection; a failure when tags were named and the host has no Tags service, or the service refuses.</returns>
    public static async Task<IGenericResult<TagSelection>> Read(
        IReadOnlyList<string> tagNames,
        ITagService? tags,
        string kind,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (tagNames.Count == 0) return GenericResult<TagSelection>.Success(None);

        if (tags is null)
        {
            return GenericResult<TagSelection>.Failure(
                TagsResultCodes.ByName("TagFilterUnavailable"), logger, TagDetails.Of("kind", kind, "tags", string.Join(", ", tagNames)));
        }

        var things = await tags.ThingsOf(kind, tagNames, cancellationToken).ConfigureAwait(false);
        return things.IsFailure || things.Value is null
            ? things.ToNewResult<TagSelection>()
            : GenericResult<TagSelection>.Success(new TagSelection(things.Value.ToHashSet()));
    }

    /// <summary>Says whether a list keeps a member: true for every member when the request named no tag, otherwise only for one that carries them all.</summary>
    /// <param name="id">The member's Id.</param>
    /// <returns>True to keep the member.</returns>
    public bool Keeps(Guid id) => _ids is null || _ids.Contains(id);

    /// <summary>Reads one page of a list, narrowed to the members that carry the tags.</summary>
    /// <typeparam name="T">The listed member.</typeparam>
    /// <param name="readPage">The list's own page read, given the rows to skip and take and the cancellation token.</param>
    /// <param name="idOf">Reads a member's Id.</param>
    /// <param name="skip">How many members precede the page.</param>
    /// <param name="take">How many members the page holds at most.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The list's own page when the request named no tag. Otherwise every member is read in the list's order, the ones without the tags are
    /// left out and the page is cut from the rest, so the page's total is how many members carry the tags.
    /// </returns>
    public async Task<IGenericResult<ConfigurationPage<T>>> Read<T>(
        Func<int, int, CancellationToken, Task<IGenericResult<ConfigurationPage<T>>>> readPage,
        Func<T, Guid> idOf,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (!Applies) return await readPage(skip, take, cancellationToken).ConfigureAwait(false);

        var every = await readPage(0, EveryRow, cancellationToken).ConfigureAwait(false);
        if (every.IsFailure || every.Value is null) return every;

        var members = new List<T>();
        await foreach (var member in every.Value.Items.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            members.Add(member);
        }

        return every.Value.Outcome.IsFailure
            ? every.Value.Outcome.ToNewResult<ConfigurationPage<T>>()
            : GenericResult<ConfigurationPage<T>>.Success(Page(members, idOf, skip, take));
    }

    /// <summary>Cuts one page from the members that carry the tags.</summary>
    /// <typeparam name="T">The listed member.</typeparam>
    /// <param name="inOrder">Every member of the list, in the order the list shows them.</param>
    /// <param name="idOf">Reads a member's Id.</param>
    /// <param name="skip">How many kept members precede the page.</param>
    /// <param name="take">How many members the page holds at most.</param>
    /// <returns>The page; its total is how many members the tags select in all.</returns>
    public ConfigurationPage<T> Page<T>(IEnumerable<T> inOrder, Func<T, Guid> idOf, int skip, int take)
    {
        var kept = inOrder.Where(member => _ids is null || _ids.Contains(idOf(member))).ToList();
        var window = kept.Skip(skip).Take(take).ToList();
        return new ConfigurationPage<T>(skip, take, skip + window.Count < kept.Count, _ => Stream(window), kept.Count);
    }

    private static async IAsyncEnumerable<T> Stream<T>(IReadOnlyList<T> members)
    {
        foreach (var member in members)
        {
            yield return member;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
