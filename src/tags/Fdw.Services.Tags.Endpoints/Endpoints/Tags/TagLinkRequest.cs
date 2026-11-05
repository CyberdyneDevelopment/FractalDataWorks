using System;

namespace Fdw.Services.Tags.Endpoints;

/// <summary>Names a tag and a thing of some kind, from the route.</summary>
public sealed class TagLinkRequest
{
    /// <summary>Gets or sets the tag's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind of the thing, a name in <c>TaggableKinds</c>.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the thing's Id.</summary>
    public Guid ThingId { get; set; }
}
