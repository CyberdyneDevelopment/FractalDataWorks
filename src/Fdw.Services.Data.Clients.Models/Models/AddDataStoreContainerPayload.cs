namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Request body for adding a container to an existing data store path.
/// </summary>
public sealed class AddDataStoreContainerPayload
{
    /// <summary>Gets or sets the path name within the data store to add the container to.</summary>
    public string PathName { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name for the new container.</summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the container type discriminator (e.g., "Table", "View").</summary>
    public string? ContainerType { get; set; }

    /// <summary>
    /// Gets or sets a JSONPath-style selector to the array of row objects in the response payload.
    /// Example: "features" for <c>response.features[*]</c>. Null means rows are at the JSON root.
    /// Only meaningful for REST containers.
    /// </summary>
    public string? RecordSelector { get; set; }

    /// <summary>
    /// Gets or sets whether nested JSON objects should be flattened into dot-notation field names.
    /// Null means the container inherits the library default (false).
    /// </summary>
    public bool? FlattenNestedObjects { get; set; }

    /// <summary>
    /// Gets or sets the separator character for flattened field names (e.g., ".").
    /// Null means the container inherits the library default (".").
    /// </summary>
    public string? FlattenSeparator { get; set; }

    /// <summary>
    /// Gets or sets whether the first row of the file is a header row. Only for a Delimited or Xlsx container; refused
    /// for any other format. Left out, the first row is read as data.
    /// </summary>
    public bool? HasHeader { get; set; }

    /// <summary>
    /// Gets or sets the separator of the file's fields. Only for a Delimited container, 1 to 10 characters; refused for
    /// any other format. It is not <c>FlattenSeparator</c>, which joins the names of flattened nested objects.
    /// </summary>
    public string? Separator { get; set; }

    /// <summary>
    /// Gets or sets the name of the worksheet to read. Only for an Xlsx container, at most 31 characters and none of
    /// <c>[ ] : * ? / \</c>; refused for any other format. Left out, a workbook with several worksheets is refused when read.
    /// </summary>
    public string? SheetName { get; set; }
}
