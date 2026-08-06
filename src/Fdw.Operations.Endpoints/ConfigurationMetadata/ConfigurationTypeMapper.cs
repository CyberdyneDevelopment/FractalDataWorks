using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>
/// Maps <see cref="IDataContainer"/> nodes to configuration metadata DTOs.
/// </summary>
/// <remarks>
/// Why: Wave C4 replaces IConfigurationType with IDataContainer as the metadata source for
/// configuration endpoints. IDataContainer provides Name and Path.Name (schema); richer
/// metadata (DisplayName, Description, ValuesFromReferences, CLR Type) is pending Wave A6
/// typed-body promotion to IDataContainer. Until then, those fields return empty/null.
/// </remarks>
internal static class ConfigurationTypeMapper
{
    /// <summary>
    /// Maps a data container to a summary DTO.
    /// </summary>
    public static ConfigurationTypeSummaryDto ToSummary(IDataContainer container) => new()
    {
        // Why: container.Name is the type discriminator (e.g., "MsSqlConnection").
        // ServiceCategory is not yet on IDataContainer — use Path.Name (schema) as a proxy.
        TypeName = container.Name,
        DisplayName = container.Name,
        Description = container.Description,
        Category = container.Parent.Name,
        // Why: ValuesFromReferences is a typed-body feature pending Wave A6. Return empty list.
        RelatedCollections = []
    };

    /// <summary>
    /// Maps a data container to a detail DTO.
    /// </summary>
    /// <remarks>
    /// Why: Property metadata requires the CLR Type from IConfigurationType (via reflection).
    /// IDataContainer does not yet expose the CLR Type — pending Wave A6. Return empty properties.
    /// </remarks>
    public static ConfigurationTypeDetailDto ToDetail(IDataContainer container) => new()
    {
        TypeName = container.Name,
        DisplayName = container.Name,
        Description = container.Description,
        Category = container.Parent.Name,
        Properties = []
    };
}
