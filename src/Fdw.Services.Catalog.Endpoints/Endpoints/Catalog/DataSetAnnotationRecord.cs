using System;
using System.Collections.Generic;
using Fdw.Data;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Database record representing a DataSet annotation with JSON-serialized tags.</summary>
[GenerateMapper]
public partial class DataSetAnnotationRecord
{
    /// <summary>Gets or sets the unique identifier of the annotation record.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of the annotated DataSet.</summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets the owner of the DataSet.</summary>
    public string? Owner { get; set; }

    /// <summary>Gets or sets the data steward responsible for the DataSet.</summary>
    public string? Steward { get; set; }

    /// <summary>Gets or sets the data classification level.</summary>
    public string? Classification { get; set; }

    /// <summary>Gets or sets the JSON-serialized list of tags.</summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>Gets or sets the date and time the record was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the date and time the record was last modified.</summary>
    public DateTime? ModifiedAt { get; set; }
}