using System.Collections.Generic;

namespace Fdw.Services.Catalog.Clients.Models;

/// <summary>
/// Request to create a new DataSet annotation.
/// </summary>
public sealed class CreateAnnotationRequest
{
    /// <summary>Gets or sets the owner of the DataSet.</summary>
    public string? Owner { get; set; }

    /// <summary>Gets or sets the data steward responsible for the DataSet.</summary>
    public string? Steward { get; set; }

    /// <summary>Gets or sets the data classification level.</summary>
    public string? Classification { get; set; }

    /// <summary>Gets or sets the tags associated with the DataSet.</summary>
    public IList<string> Tags { get; set; } = new List<string>();
}
