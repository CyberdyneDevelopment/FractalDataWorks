using System;
using System.Collections.Generic;
using Fdw.Data;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Database record representing a data set configuration.
/// </summary>
[GenerateMapper]
public partial class DataSetRecord
{
    /// <summary>Gets or sets the data set identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the data set name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the data set description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the data set version.</summary>
    public string Version { get; set; } = "1.0";
    /// <summary>Gets or sets the data set category.</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>Gets or sets the fully qualified record type name.</summary>
    public string? RecordTypeName { get; set; }
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the last modification timestamp.</summary>
    public DateTime? ModifiedAt { get; set; }
}