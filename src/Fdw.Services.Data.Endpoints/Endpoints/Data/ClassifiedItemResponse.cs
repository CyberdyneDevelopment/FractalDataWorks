using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Response DTO naming a classified item (a data store path, container or field, or a data set field) and its data classification.
/// </summary>
public class ClassifiedItemResponse
{
    /// <summary>Gets or sets the item's durable id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets what the item is: Path, Container or Field.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the item's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the item's <c>ClassificationId</c>; <c>NotYetClassified</c> until someone picks a level.</summary>
    public int ClassificationId { get; set; }
}
