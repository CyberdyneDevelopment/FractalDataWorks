namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Column definition for a preview response.
/// </summary>
public sealed class PreviewColumnDto
{
    /// <summary>Gets or sets the column name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the data type name.</summary>
    public string? DataType { get; set; }

    /// <summary>Gets or sets whether the column is nullable.</summary>
    public bool IsNullable { get; set; }
}
