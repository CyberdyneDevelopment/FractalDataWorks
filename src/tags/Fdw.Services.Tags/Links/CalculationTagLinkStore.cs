using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Tags.Links;

/// <summary>Reads and writes <c>CalculationTag</c>, the links between Calculations and tags.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TagLinkStores), "Calculation")]
public sealed class CalculationTagLinkStore : TagLinkStoreBase<CalculationTagConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="CalculationTagLinkStore"/> class.</summary>
    public CalculationTagLinkStore()
        : base("Calculation")
    {
    }

    /// <inheritdoc />
    protected override CalculationTagConfiguration Make(Guid thingId, Guid tagId)
        => new() { Id = Guid.CreateVersion7(), CalculationId = thingId, TagId = tagId, CreateDate = DateTimeOffset.UtcNow };

    /// <inheritdoc />
    protected override TagLinkRow Row(CalculationTagConfiguration link) => new(link.Id, link.CalculationId, link.TagId);
}
