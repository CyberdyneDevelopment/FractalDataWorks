using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Tags.Abstractions;
using Fdw.Services.Tags.Configuration;

namespace Fdw.Services.Tags;

/// <summary>Decides whether a tag may be put on a thing, from where the tag sits and where the thing sits.</summary>
public interface ITagScope
{
    /// <summary>Checks that <paramref name="tag"/> may be put on the thing.</summary>
    /// <param name="tag">The tag, with its owner.</param>
    /// <param name="kind">The kind of the thing.</param>
    /// <param name="thingId">The thing.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or the refusal that names why.</returns>
    Task<IGenericResult> Allows(TagDomainConfiguration tag, ITaggableKind kind, Guid thingId, CancellationToken cancellationToken = default);
}
