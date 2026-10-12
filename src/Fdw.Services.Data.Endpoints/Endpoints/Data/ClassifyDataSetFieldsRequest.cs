using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for setting the data classification of one or more fields of a data set.
/// </summary>
public class ClassifyDataSetFieldsRequest
{
    /// <summary>Gets or sets the data set name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the durable ids of the fields to classify; one or more.</summary>
    public List<Guid> FieldIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the <c>ClassificationId</c> of a <c>DataClassifications</c> level, required. The whole body is
    /// the classification: to unclassify the fields, send <c>NotYetClassified</c>'s id.
    /// </summary>
    public int ClassificationId { get; set; }
}
