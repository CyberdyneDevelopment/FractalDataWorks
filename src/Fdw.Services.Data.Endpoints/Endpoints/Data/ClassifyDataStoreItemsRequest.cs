using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for setting the data classification of one or more paths, containers or fields of a data store.
/// </summary>
public class ClassifyDataStoreItemsRequest
{
    /// <summary>Gets or sets the data store name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the durable ids of the paths, containers and fields to classify; one or more.</summary>
    public List<Guid> ItemIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the <c>ClassificationId</c> of a <c>DataClassifications</c> level, required. The whole body is
    /// the classification: to unclassify the items, send <c>NotYetClassified</c>'s id.
    /// </summary>
    public int ClassificationId { get; set; }
}
