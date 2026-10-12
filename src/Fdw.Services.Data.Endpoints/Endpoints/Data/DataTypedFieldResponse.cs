using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Response DTO naming a field whose portable data type was set, and that type (FDW-1103).</summary>
public class DataTypedFieldResponse
{
    /// <summary>Gets or sets the field's durable id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the field's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the field's portable data type.</summary>
    public string? FrameworkDataType { get; set; }
}
