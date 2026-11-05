using Fdw.Services.Catalog.Clients.Models;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Builds the payload a DataSet annotation is answered as; the one mapping every annotation read shares. Its tags are the data set's, read by <see cref="DataSetAnnotationTags"/>.</summary>
internal static class DataSetAnnotationPayloadMapper
{
    /// <summary>Maps a DataSetAnnotationImplementationConfiguration to its payload.</summary>
    /// <param name="config">The annotation.</param>
    /// <returns>The payload.</returns>
    internal static DataSetAnnotationPayload Map(IDataSetAnnotationImplementationConfiguration config)
        => new()
        {
            DataSetName = config.DataSetName,
            Owner = config.BusinessOwner,
            Steward = config.TechnicalOwner,
            Classification = config.DataClassification,
            IsResolved = config.IsResolved,
            ResolvedAt = config.ResolvedDate,
            ResolvedBy = config.ResolvedBy
        };
}
