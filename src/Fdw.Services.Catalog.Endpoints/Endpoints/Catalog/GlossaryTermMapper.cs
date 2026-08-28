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
    internal static GlossaryTermConfiguration MapFromDto(GlossaryTermResponse dto)
        => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            Definition = dto.Definition,
            Category = dto.Category,
            Owner = dto.Owner ?? string.Empty,
            Steward = string.Empty
        };
}
