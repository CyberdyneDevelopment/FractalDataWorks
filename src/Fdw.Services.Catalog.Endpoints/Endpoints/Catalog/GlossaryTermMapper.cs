using System;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// Maps between <see cref="GlossaryTermResponse"/> and <see cref="GlossaryTermImplementationConfiguration"/>.
/// </summary>
/// <remarks>
/// Why: Mapper lives in Catalog.Endpoints (alongside GlossaryTermResponse) rather than in
/// Services.Quality — Services.Quality must not depend on Catalog.Endpoints DTOs.
/// </remarks>
internal static class GlossaryTermMapper
{
    /// <summary>Maps a GlossaryTermResponse to a GlossaryTermImplementationConfiguration for upsert via the provider.</summary>
    internal static GlossaryTermImplementationConfiguration MapFromDto(GlossaryTermResponse dto, Guid? categoryId)
        => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            Definition = dto.Definition,
            CategoryId = categoryId,
            Owner = dto.Owner ?? string.Empty,
            Steward = string.Empty,
            StandsFor = dto.StandsFor,
            IsApproved = dto.IsApproved
        };

    /// <summary>Maps a GlossaryTermImplementationConfiguration to its response DTO.</summary>
    internal static GlossaryTermResponse ToDto(Fdw.Services.Quality.Configuration.IGlossaryTermImplementationConfiguration config)
        => new()
        {
            Id = config.Id,
            Name = config.Name,
            Definition = config.Definition,
            Owner = config.Owner,
            StandsFor = config.StandsFor,
            IsApproved = config.IsApproved
        };

    /// <summary>
    /// Merges an <see cref="UpdateGlossaryTermRequest"/> onto an already-loaded configuration in
    /// place — an omitted (null) request property leaves the existing value untouched. Never touches
    /// Steward, Formula, RelatedTerms or LinkedDataSets, none of which this update surface exposes.
    /// </summary>
    internal static void ApplyDto(Fdw.Services.Quality.Configuration.IGlossaryTermImplementationConfiguration config, UpdateGlossaryTermRequest request, Guid? categoryId)
    {
        if (request.Name is not null) config.Name = request.Name;
        if (request.Definition is not null) config.Definition = request.Definition;
        if (request.Category is not null) config.CategoryId = categoryId;
        if (request.Owner is not null) config.Owner = request.Owner;
        if (request.StandsFor is not null) config.StandsFor = request.StandsFor;
        if (request.IsApproved is not null) config.IsApproved = request.IsApproved;
    }
}
