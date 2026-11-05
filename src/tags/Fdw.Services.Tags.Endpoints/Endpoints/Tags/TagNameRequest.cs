namespace Fdw.Services.Tags.Endpoints;

/// <summary>A request that names one tag, from the route.</summary>
public sealed class TagNameRequest
{
    /// <summary>Gets or sets the tag's name.</summary>
    public string Name { get; set; } = string.Empty;
}
