using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Tags.Abstractions;
using Fdw.Services.Tags.Configuration;

namespace Fdw.Services.Tags;

/// <summary>The one entry for tags: make and remove them, and put them on and take them off things of any taggable kind.</summary>
public interface ITagService
{
    /// <summary>Makes a tag. Its name must be new across the tenant's tenant, organization and workspace tags.</summary>
    /// <param name="name">The tag's name.</param>
    /// <param name="ownerKind">Whose tag it is: one of <see cref="TagOwnerKinds"/>.</param>
    /// <param name="ownerId">The Id of the tenant, organization or workspace that owns it.</param>
    /// <param name="description">What the tag means, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tag as the list shows it.</returns>
    Task<IGenericResult<TagDomainConfiguration>> Create(string name, string ownerKind, Guid ownerId, string? description, CancellationToken cancellationToken = default);

    /// <summary>Deletes a tag: its links are retired kind by kind, then the tag. Every step is safe to repeat.</summary>
    /// <param name="name">The tag's name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The outcome.</returns>
    Task<IGenericResult> Delete(string name, CancellationToken cancellationToken = default);

    /// <summary>Puts a tag on a thing.</summary>
    /// <param name="kind">The kind of the thing, a name in <c>TaggableKinds</c>.</param>
    /// <param name="thingId">The thing.</param>
    /// <param name="tagName">The tag's name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The outcome.</returns>
    Task<IGenericResult> Link(string kind, Guid thingId, string tagName, CancellationToken cancellationToken = default);

    /// <summary>Takes a tag off a thing.</summary>
    /// <param name="kind">The kind of the thing, a name in <c>TaggableKinds</c>.</param>
    /// <param name="thingId">The thing.</param>
    /// <param name="tagName">The tag's name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The outcome.</returns>
    Task<IGenericResult> Unlink(string kind, Guid thingId, string tagName, CancellationToken cancellationToken = default);

    /// <summary>Lists the names of the tags a thing carries.</summary>
    /// <param name="kind">The kind of the thing, a name in <c>TaggableKinds</c>.</param>
    /// <param name="thingId">The thing.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tag names.</returns>
    Task<IGenericResult<IReadOnlyList<string>>> TagsOf(string kind, Guid thingId, CancellationToken cancellationToken = default);

    /// <summary>Reads one tag, as the list shows it.</summary>
    /// <param name="name">The tag's name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tag; a failure that says so when no tag has the name.</returns>
    Task<IGenericResult<TagDomainConfiguration>> Get(string name, CancellationToken cancellationToken = default);

    /// <summary>Lists the tags of every thing of a kind that carries any, in one read of the links and one of the tags.</summary>
    /// <param name="kind">The kind of the things, a name in <c>TaggableKinds</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>For each thing that carries a tag, its tag names in name order; a thing with none is absent.</returns>
    Task<IGenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<string>>>> TagsByThing(string kind, CancellationToken cancellationToken = default);

    /// <summary>Lists the things of a kind that carry every one of the named tags.</summary>
    /// <param name="kind">The kind of the things, a name in <c>TaggableKinds</c>.</param>
    /// <param name="tagNames">The tags, all of which a thing must carry.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The Ids of the things.</returns>
    Task<IGenericResult<IReadOnlyList<Guid>>> ThingsOf(string kind, IReadOnlyList<string> tagNames, CancellationToken cancellationToken = default);
}
