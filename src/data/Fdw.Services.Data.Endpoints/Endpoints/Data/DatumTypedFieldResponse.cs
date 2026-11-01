using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Response DTO naming a field whose datum type was set, and that type (FDW-988).</summary>
public class DatumTypedFieldResponse
{
    /// <summary>Gets or sets the field's durable id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the field's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the field's <c>DatumTypeId</c>.</summary>
    public int DatumTypeId { get; set; }
}
