using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Data.Caching;

/// <summary>
/// One row of the cache change tokens (FDW-4782): the last change published for one invalidation tag. Property names
/// match the column names exactly.
/// </summary>
/// <remarks>
/// A host that writes configuration sets a new <see cref="Token"/> for each tag it changed; every host with a result
/// cache reads the rows now and then and drops a tag whose token is not the one it saw last. The token is compared
/// for equality only, so two writes are never ordered; it is minted by the code (no column default), like every Id.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class CacheChangeToken
{
    /// <summary>Gets or sets the invalidation tag, <c>{Path}.{Container}</c> (e.g. <c>conn.Connection</c>).</summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>Gets or sets the token of the tag's last published change.</summary>
    public Guid Token { get; set; }

    /// <summary>Gets or sets when the change was published.</summary>
    public DateTimeOffset ChangedAt { get; set; }
}
