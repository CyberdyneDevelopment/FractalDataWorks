using System;

namespace Fdw.Services.Tags.Endpoints;

/// <summary>Names a thing of some kind, from the route.</summary>
public sealed class ThingTagsRequest
{
    /// <summary>Gets or sets the kind of the thing, a name in <c>TaggableKinds</c>.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the thing's Id.</summary>
    public Guid ThingId { get; set; }
}
