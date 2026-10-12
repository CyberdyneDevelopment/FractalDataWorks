using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Maps an annotation payload onto the configuration that is written.</summary>
/// <remarks>
/// On the endpoint side rather than the provider: shaping a payload into a configuration is what
/// an endpoint does, and a provider carries no logic of its own.
/// </remarks>
internal static class DataSetAnnotationRequestMapper
{
    /// <summary>Builds an annotation configuration from the parts a payload carries.</summary>
    internal static DataSetAnnotationImplementationConfiguration FromPayload(
        string dataSetName, string? owner, string? steward, string? classification, IEnumerable<string>? tags)
        => new()
        {
            Name = dataSetName,
            DataSetName = dataSetName,
            BusinessOwner = owner,
            TechnicalOwner = steward,
            DataClassification = classification,
            Tags = tags?.Select(t => new DataSetAnnotationTagConfiguration { Tag = t, Name = t }).ToList() ?? []
        };
}
