using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Quality.Configuration;

/// <summary>
/// Configuration for DataSet metadata annotations.
/// Stored in catalog.DataSetAnnotation table.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Catalog",
    ServiceType = "Annotation")]
// Why: IGenericConfiguration is required by ImplementationConfigurationProviderBase<T>
// for dual-source (ctrl+cfg) provider pattern.
public sealed partial class DataSetAnnotationConfiguration : IGenericConfiguration
{
    /// <inheritdoc />
    public string SectionName => "Catalogs";

    /// <inheritdoc />
    // Why: Matches ServiceCategory from [ManagedConfiguration] attribute for IOptions binding path.
    public string ServiceType => "Catalog";

    /// <inheritdoc />
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the display name for this annotation.
    /// </summary>
    public string Name { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets the unique identifier for this annotation.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the DataSet being annotated.
    /// </summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the business description of the DataSet.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the business owner of the DataSet.
    /// </summary>
    public string? BusinessOwner { get; set; }

    /// <summary>
    /// Gets or sets the technical owner of the DataSet.
    /// </summary>
    public string? TechnicalOwner { get; set; }

    /// <summary>
    /// Gets or sets the update frequency (e.g., "Daily", "Hourly", "Real-time").
    /// </summary>
    public string? UpdateFrequency { get; set; }

    /// <summary>
    /// Gets or sets the data classification level (e.g., "Public", "Internal", "Confidential").
    /// </summary>
    public string? DataClassification { get; set; }

    /// <summary>
    /// Gets or sets the tags for this annotation.
    /// </summary>
    public IList<DataSetAnnotationTagConfiguration> Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets the field descriptions for this annotation.
    /// </summary>
    public IList<DataSetAnnotationFieldDescriptionConfiguration> FieldDescriptions { get; set; } = [];

    /// <summary>
    /// Gets or sets the field business names for this annotation.
    /// </summary>
    public IList<DataSetAnnotationFieldBusinessNameConfiguration> FieldBusinessNames { get; set; } = [];
}
