using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// One container of a path in a data store being created, with its fields and keys.
/// </summary>
/// <remarks>
/// The same shape as adding a container to an existing path (<see cref="AddDataStoreContainerRequest"/>),
/// less the store and path names the enclosing request already carries. A Foreign key may reference
/// any container in the same request, by name, wherever it appears in the tree.
/// </remarks>
public class CreateDataStorePathContainerRequest
{
    /// <summary>Gets or sets the container's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the container type discriminator (e.g., "Table", "View").</summary>
    public string? ContainerType { get; set; }

    /// <summary>
    /// Gets or sets the serialization format of the container's payload (e.g., "Json", "Xml"). Null means
    /// inherit the owning connection transport's declared default format.
    /// </summary>
    public string? Format { get; set; }

    /// <summary>Gets or sets a JSONPath-style selector to the array of row objects; only meaningful for REST containers.</summary>
    public string? RecordSelector { get; set; }

    /// <summary>Gets or sets whether nested JSON objects are flattened into dot-notation field names.</summary>
    public bool? FlattenNestedObjects { get; set; }

    /// <summary>Gets or sets the separator for flattened field names.</summary>
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

    /// <summary>
    /// Gets or sets the path, after the connection's address, that the rows are sent to when this container is the destination of
    /// an HTTP connection, for example <c>/api/v1/ingest</c>. A full web address is refused. Left out, a destination that needs it
    /// fails naming it when it runs.
    /// </summary>
    public string? WriteEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the HTTP method the rows are sent with: <c>POST</c> or <c>PUT</c>; anything else is refused. Left out, a
    /// destination that needs it fails naming it when it runs.
    /// </summary>
    public string? WriteMethod { get; set; }

    /// <summary>
    /// Gets or sets the content type the rows are sent as, for example <c>application/json</c>; a value that is not a media type is
    /// refused. Left out, a destination that needs it fails naming it when it runs.
    /// </summary>
    public string? WriteContentType { get; set; }

    /// <summary>
    /// Gets or sets the <c>ClassificationId</c> of the container's data classification level (see
    /// <c>GET /data-classifications</c>). Left out, it is <c>NotYetClassified</c>. An id that is not a level is refused.
    /// </summary>
    public int ClassificationId { get; set; } = DataClassifications.NotYetClassified.Id;

    /// <summary>Gets or sets the field (column) definitions.</summary>
    public IList<CreateDataStoreContainerFieldRequest> Fields { get; set; } = [];

    /// <summary>Gets or sets the keys, resolved to fields by name.</summary>
    public IList<CreateDataStoreContainerKeyRequest> Keys { get; set; } = [];
}
