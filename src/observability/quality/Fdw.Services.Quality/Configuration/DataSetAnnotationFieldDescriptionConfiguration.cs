using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Quality.Configuration;

/// <summary>
/// Configuration for a data set annotation field description.
/// Child of DataSetAnnotationImplementationConfiguration.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Catalog")]
public sealed partial class DataSetAnnotationFieldDescriptionConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name for display/binding.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field name.
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field description.
    /// </summary>
    // Why [NotStored]: the description is kept in the descriptions table, in the language of the caller's tenant (FDW-6208), so the column is gone and no read selects it and no save writes it; the
    // provider fills the member on every read that composes the record. The member stays, so every caller of Description still gets the text.
    [NotStored]
    public string Description { get; set; } = string.Empty;
}
