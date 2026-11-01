using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Replaces one named sample's rows for a data set.</summary>
/// <remarks>
/// Full-replace on every call: an upsert with the same <see cref="SampleName"/> retires whichever
/// version was current and inserts a wholly new one, rows and all — there is no per-row PATCH.
/// </remarks>
public class DataSetSampleRequest
{
    /// <summary>Gets or sets the data set name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the sample's own name.</summary>
    public string SampleName { get; set; } = string.Empty;

    /// <summary>Gets or sets what this sample is for.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether this is the data set's default sample.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Gets or sets the sample's rows, each a field-name-to-value map. A missing field name is
    /// omitted from that row; a present field name mapped to null is a deliberately empty cell.</summary>
    public List<Dictionary<string, string?>> Rows { get; set; } = [];
}
