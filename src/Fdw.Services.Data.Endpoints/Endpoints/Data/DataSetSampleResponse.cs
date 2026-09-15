using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>One named sample for a data set, as it stands after a list or an upsert.</summary>
public class DataSetSampleResponse
{
    /// <summary>Gets or sets the sample's own name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what this sample is for.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether this is the data set's default sample.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Gets or sets the sample's rows, each a field-name-to-value map.</summary>
    public List<Dictionary<string, string?>> Rows { get; set; } = [];
}
