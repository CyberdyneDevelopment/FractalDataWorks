namespace Fdw.Schema.Endpoints.Discovery;

/// <summary>
/// Column definition for data preview.
/// </summary>
public class PreviewColumnDto
{
    /// <summary>
    /// Gets or sets the column name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data type.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the column is part of the key.
    /// </summary>
    public bool IsKey { get; set; }
}
