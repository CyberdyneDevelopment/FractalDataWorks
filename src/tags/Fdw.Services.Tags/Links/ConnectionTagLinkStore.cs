using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Tags.Links;

/// <summary>Reads and writes <c>ConnectionTag</c>, the links between Connections and tags.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TagLinkStores), "Connection")]
public sealed class ConnectionTagLinkStore : TagLinkStoreBase<ConnectionTagConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionTagLinkStore"/> class.</summary>
    public ConnectionTagLinkStore()
        : base("Connection")
    {
    }

    /// <inheritdoc />
    protected override ConnectionTagConfiguration Make(Guid thingId, Guid tagId)
        => new() { Id = Guid.CreateVersion7(), ConnectionId = thingId, TagId = tagId, CreateDate = DateTimeOffset.UtcNow };

    /// <inheritdoc />
    protected override TagLinkRow Row(ConnectionTagConfiguration link) => new(link.Id, link.ConnectionId, link.TagId);
}
