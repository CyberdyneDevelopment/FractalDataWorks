using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Tags.Links;

/// <summary>Reads and writes <c>PipelineTag</c>, the links between Pipelines and tags.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TagLinkStores), "Pipeline")]
public sealed class PipelineTagLinkStore : TagLinkStoreBase<PipelineTagConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="PipelineTagLinkStore"/> class.</summary>
    public PipelineTagLinkStore()
        : base("Pipeline")
    {
    }

    /// <inheritdoc />
    protected override PipelineTagConfiguration Make(Guid thingId, Guid tagId)
        => new() { Id = Guid.CreateVersion7(), PipelineId = thingId, TagId = tagId, CreateDate = DateTimeOffset.UtcNow };

    /// <inheritdoc />
    protected override TagLinkRow Row(PipelineTagConfiguration link) => new(link.Id, link.PipelineId, link.TagId);
}
