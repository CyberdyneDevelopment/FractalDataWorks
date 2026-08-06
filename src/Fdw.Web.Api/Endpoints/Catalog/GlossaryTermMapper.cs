using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// Maps between <see cref="GlossaryTermResponse"/> and <see cref="GlossaryTermConfiguration"/>.
/// </summary>
/// <remarks>
/// Why: Mapper lives in Catalog.Endpoints (alongside GlossaryTermResponse) rather than in
/// Services.Quality — Services.Quality must not depend on Catalog.Endpoints DTOs.
/// </remarks>
internal static class GlossaryTermMapper
{
    /// <summary>Maps a GlossaryTermResponse to a GlossaryTermConfiguration for upsert via the provider.</summary>
    // Why: GlossaryTermResponse.Owner → GlossaryTermConfiguration.Owner (direct mapping, same semantics).
    // Why: GlossaryTermResponse.RelatedDataSets has no counterpart on GlossaryTermConfiguration.LinkedDataSets
    //      (which stores GlossaryTermLinkedDataSetConfiguration child objects, not plain strings).
    //      Left unmapped — the string list from the DTO has insufficient detail to round-trip.
    internal static GlossaryTermConfiguration MapFromDto(GlossaryTermResponse dto)
        => new()
        {
            // Why: If dto.Id is non-empty the provider treats it as an update; if empty it mints UUIDv7.
            Id = dto.Id,
            Name = dto.Name,
            Definition = dto.Definition,
            Category = dto.Category,
            Owner = dto.Owner ?? string.Empty,
            Steward = string.Empty
        };
}
