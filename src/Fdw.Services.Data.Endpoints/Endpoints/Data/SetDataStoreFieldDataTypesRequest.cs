using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Request DTO for setting the portable data type of one or more fields of a data store (FDW-1103).</summary>
public class SetDataStoreFieldDataTypesRequest
{
    /// <summary>Gets or sets the data store name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the durable ids of the container fields to type; one or more.</summary>
    public List<Guid> FieldIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the portable data type to give every named field, required: the name of a <c>DataTypes</c>
    /// option, listed by <c>GET /data-types</c>.
    /// </summary>
    public string FrameworkDataType { get; set; } = string.Empty;
}
