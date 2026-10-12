namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Column definition returned in a DataSet preview response.
/// The JSON shape matches <c>ColumnSchemaPayload</c> in the client package.
/// </summary>
public class DataSetPreviewColumnDto
{
    /// <summary>Gets or sets the column name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the data type.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the column allows nulls.</summary>
    public bool IsNullable { get; set; }

    /// <summary>Gets or sets the maximum length for string columns.</summary>
    public int? MaxLength { get; set; }

    /// <summary>Gets or sets the inferred property role.</summary>
    public string? Role { get; set; }
}
