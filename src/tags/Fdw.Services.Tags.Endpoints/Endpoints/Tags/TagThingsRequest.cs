namespace Fdw.Services.Tags.Endpoints;

/// <summary>Names a tag and a kind, from the route.</summary>
public sealed class TagThingsRequest
{
    /// <summary>Gets or sets the tag's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind of the things to list, a name in <c>TaggableKinds</c>.</summary>
    public string Kind { get; set; } = string.Empty;
}
