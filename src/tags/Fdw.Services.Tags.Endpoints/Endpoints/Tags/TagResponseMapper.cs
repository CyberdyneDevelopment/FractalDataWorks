using Fdw.Services.Tags.Configuration;

namespace Fdw.Services.Tags.Endpoints;

/// <summary>Maps tag records to the responses the API returns.</summary>
internal static class TagResponseMapper
{
    /// <summary>Maps a tag implementation, whose list fields the provider stamps on read.</summary>
    /// <param name="tag">The tag.</param>
    /// <returns>The response.</returns>
    public static TagResponse From(ITagImplementationConfiguration tag)
        => new() { Name = tag.Name, OwnerKind = tag.OwnerKind, OwnerId = tag.OwnerId, Description = tag.Description };

    /// <summary>Maps a tag as the list shows it.</summary>
    /// <param name="tag">The domain record.</param>
    /// <returns>The response.</returns>
    public static TagResponse From(TagDomainConfiguration tag)
        => new() { Name = tag.Name, OwnerKind = tag.OwnerKind, OwnerId = tag.OwnerId };
}
