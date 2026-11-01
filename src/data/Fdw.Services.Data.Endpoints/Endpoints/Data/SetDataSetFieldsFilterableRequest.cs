using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for choosing whether API callers may filter on one or more fields of a data set (FDW-1048).
/// </summary>
public class SetDataSetFieldsFilterableRequest
{
    /// <summary>Gets or sets the data set name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the durable ids of the fields to change; one or more.</summary>
    public List<Guid> FieldIds { get; set; } = [];

    /// <summary>
    /// Gets or sets whether callers may filter on the fields. The whole body is the choice: <see langword="false"/> refuses a
    /// caller's filter on them, <see langword="true"/> allows it again. A view's own stored filter is not limited by it.
    /// </summary>
    public bool IsFilterable { get; set; }
}
