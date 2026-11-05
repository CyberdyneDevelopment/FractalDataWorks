using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Tags.Links;

/// <summary>Reads and writes <c>DataStoreTag</c>, the links between DataStores and tags.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TagLinkStores), "DataStore")]
public sealed class DataStoreTagLinkStore : TagLinkStoreBase<DataStoreTagConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataStoreTagLinkStore"/> class.</summary>
    public DataStoreTagLinkStore()
        : base("DataStore")
    {
    }

    /// <inheritdoc />
    protected override DataStoreTagConfiguration Make(Guid thingId, Guid tagId)
        => new() { Id = Guid.CreateVersion7(), DataStoreId = thingId, TagId = tagId, CreateDate = DateTimeOffset.UtcNow };

    /// <inheritdoc />
    protected override TagLinkRow Row(DataStoreTagConfiguration link) => new(link.Id, link.DataStoreId, link.TagId);
}
