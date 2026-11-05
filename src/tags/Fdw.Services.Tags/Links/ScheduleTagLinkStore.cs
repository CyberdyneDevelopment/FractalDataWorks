using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Tags.Links;

/// <summary>Reads and writes <c>ScheduleTag</c>, the links between Schedules and tags.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TagLinkStores), "Schedule")]
public sealed class ScheduleTagLinkStore : TagLinkStoreBase<ScheduleTagConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ScheduleTagLinkStore"/> class.</summary>
    public ScheduleTagLinkStore()
        : base("Schedule")
    {
    }

    /// <inheritdoc />
    protected override ScheduleTagConfiguration Make(Guid thingId, Guid tagId)
        => new() { Id = Guid.CreateVersion7(), ScheduleId = thingId, TagId = tagId, CreateDate = DateTimeOffset.UtcNow };

    /// <inheritdoc />
    protected override TagLinkRow Row(ScheduleTagConfiguration link) => new(link.Id, link.ScheduleId, link.TagId);
}
