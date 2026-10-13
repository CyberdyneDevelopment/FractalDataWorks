using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Detailed DTO for a data container, including field information.
/// </summary>
public class DataStoreContainerDetailDto
{
    /// <summary>Gets or sets the container identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the container name (table/collection name).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the container type (e.g., Table, View).</summary>
    public string ContainerType { get; set; } = string.Empty;

    /// <summary>Gets or sets the container description.</summary>
    public string? Description { get; set; }

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

    /// <summary>Gets or sets the fields within this container.</summary>
    public IList<DataStoreFieldResponse> Fields { get; set; } = [];
}
