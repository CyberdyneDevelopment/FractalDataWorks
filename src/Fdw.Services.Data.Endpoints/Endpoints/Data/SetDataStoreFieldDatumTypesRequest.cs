using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Request DTO for setting the datum type of one or more fields of a data store (FDW-988).</summary>
public class SetDataStoreFieldDatumTypesRequest
{
    /// <summary>Gets or sets the data store name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the durable ids of the container fields to type; one or more.</summary>
    public List<Guid> FieldIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the <c>DatumTypeId</c> to give every named field, required. To untype the fields, send
    /// <c>NotYetTyped</c>'s id.
    /// </summary>
    public int DatumTypeId { get; set; }
}
