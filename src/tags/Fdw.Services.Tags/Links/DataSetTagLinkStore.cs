using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Tags.Links;

/// <summary>Reads and writes <c>DataSetTag</c>, the links between DataSets and tags.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TagLinkStores), "DataSet")]
public sealed class DataSetTagLinkStore : TagLinkStoreBase<DataSetTagConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataSetTagLinkStore"/> class.</summary>
    public DataSetTagLinkStore()
        : base("DataSet")
    {
    }

    /// <inheritdoc />
    protected override DataSetTagConfiguration Make(Guid thingId, Guid tagId)
        => new() { Id = Guid.CreateVersion7(), DataSetId = thingId, TagId = tagId, CreateDate = DateTimeOffset.UtcNow };

    /// <inheritdoc />
    protected override TagLinkRow Row(DataSetTagConfiguration link) => new(link.Id, link.DataSetId, link.TagId);
}
