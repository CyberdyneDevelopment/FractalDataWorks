namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request for previewing data from a DataSet.
/// </summary>
public sealed class PreviewDataSetRequest
{
    /// <summary>Gets or sets the DataSet name to preview.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the maximum number of rows to return. Defaults to 100.</summary>
    public int MaxRows { get; set; } = 100;
}
