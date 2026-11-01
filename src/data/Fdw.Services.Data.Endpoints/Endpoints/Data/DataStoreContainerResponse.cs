using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Summary DTO for a data container within a path.
/// </summary>
public class DataStoreContainerResponse
{
    /// <summary>Gets or sets the container identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the container name (table/collection name).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the container type (e.g., Table, View).</summary>
    public string ContainerType { get; set; } = string.Empty;

    /// <summary>Gets or sets the source-discovered description.</summary>
    public string? SourceDescription { get; set; }

    /// <summary>
    /// Gets or sets the <c>ClassificationId</c> of the container's data classification level; <c>NotYetClassified</c> until someone picks a level.
    /// </summary>
    public int ClassificationId { get; set; }

    /// <summary>Gets or sets the container's serialization format, or null when it states none.</summary>
    public string? Format { get; set; }

    /// <summary>Gets or sets whether the file's first row is a header row, or null when not stated.</summary>
    public bool? HasHeader { get; set; }

    /// <summary>Gets or sets the separator of the file's fields, or null when not stated.</summary>
    public string? Separator { get; set; }

    /// <summary>Gets or sets the worksheet to read, or null when not stated.</summary>
    public string? SheetName { get; set; }

    /// <summary>Gets or sets the path the rows are sent to when this container is an HTTP destination, or null when not stated.</summary>
    public string? WriteEndpoint { get; set; }

    /// <summary>Gets or sets the HTTP method the rows are sent with, or null when not stated.</summary>
    public string? WriteMethod { get; set; }

    /// <summary>Gets or sets the content type the rows are sent as, or null when not stated.</summary>
    public string? WriteContentType { get; set; }

    /// <summary>Gets or sets the auto-generated key field names.</summary>
    public IList<string> SurrogateKeyFields { get; set; } = [];

    /// <summary>Gets or sets the business key field names.</summary>
    public IList<string> NaturalKeyFields { get; set; } = [];

    /// <summary>Gets or sets the fields within the container.</summary>
    public IList<DataStoreFieldResponse> Fields { get; set; } = [];
}
