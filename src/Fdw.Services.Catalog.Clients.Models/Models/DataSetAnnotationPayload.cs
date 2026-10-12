using System.Collections.Generic;

namespace Fdw.Services.Catalog.Clients.Models;

/// <summary>
/// Data transfer object representing a DataSet annotation.
/// </summary>
public sealed class DataSetAnnotationPayload
{
    /// <summary>Gets or sets the name of the annotated DataSet.</summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets the owner of the DataSet.</summary>
    public string? Owner { get; set; }

    /// <summary>Gets or sets the data steward responsible for the DataSet.</summary>
    public string? Steward { get; set; }

    /// <summary>Gets or sets the data classification level.</summary>
    public string? Classification { get; set; }

    /// <summary>Gets or sets the tags associated with the DataSet.</summary>
    public IList<string> Tags { get; set; } = new List<string>();
}
