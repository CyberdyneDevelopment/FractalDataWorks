using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Quality.Configuration;

/// <summary>
/// Configuration for a data set annotation tag.
/// Child of DataSetAnnotationConfiguration.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Catalog")]
public sealed partial class DataSetAnnotationTagConfiguration : IGenericConfiguration
{
    /// <inheritdoc/>
    public string SectionName => "Catalogs";

    /// <inheritdoc/>
    public string ServiceType => "Catalog";

    /// <inheritdoc/>
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the logical FK to the owning DataSetAnnotation (catalog.DataSetAnnotation.Id). The
    /// configuration save translator resolves the physical DataSetAnnotationRowId from this via subquery.
    /// </summary>
    public Guid DataSetAnnotationId { get; set; }

    /// <summary>
    /// Gets or sets the name for display/binding.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tag value.
    /// </summary>
    public string Tag { get; set; } = string.Empty;
}
